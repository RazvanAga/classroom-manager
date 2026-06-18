using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Groups;
using Classroom.Domain.Students;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Groups;

public static class GroupsEndpoints
{
    public static IEndpointRouteBuilder MapGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        // A teacher tool, scoped to a class the teacher must be a member of (kiosk principals are
        // rejected — this is not one of the widened roster-read/shop/equip endpoints, design.md §5.4).
        var group = app.MapGroup("/api/classes/{classId:guid}/groupings")
            .WithTags("Groups")
            .RequireAuthorization();

        group.MapPost("/preview", PreviewAsync)
            .AddEndpointFilter<ValidationFilter<FormGroupingRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Form even (optionally gender-balanced) random groups without saving them.");

        group.MapPost("/", SaveAsync)
            .AddEndpointFilter<ValidationFilter<SaveGroupingRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Save a formed grouping (re-derived from its seed) to reuse during an activity.");

        group.MapGet("/", ListAsync)
            .WithSummary("List the class's saved groupings (most recent first).");

        group.MapGet("/{groupingId:guid}", GetAsync)
            .WithSummary("Retrieve a saved grouping with its full membership.");

        return app;
    }

    private static async Task<IResult> PreviewAsync(
        Guid classId,
        FormGroupingRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var roster = await LoadRosterAsync(db, classId);
        if (roster.Count == 0)
        {
            return NoStudents();
        }

        // A fresh seed makes each preview a new arrangement; returning it lets the client save this
        // exact one. The fairness/evenness rule lives in the pure, unit-tested domain function.
        var seed = Random.Shared.Next();
        var groups = GroupFormer.Form(ToGroupingStudents(roster), request.GroupSize, request.BalanceByGender, seed);

        return Results.Ok(new FormedGroupingResponse(
            request.GroupSize, request.BalanceByGender, seed, ToGroupResponses(groups, roster)));
    }

    private static async Task<IResult> SaveAsync(
        Guid classId,
        SaveGroupingRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var roster = await LoadRosterAsync(db, classId);
        if (roster.Count == 0)
        {
            return NoStudents();
        }

        // Re-derive the previewed arrangement from its seed (grouping is deterministic), so the saved
        // result equals what the teacher saw — without trusting any client-supplied membership.
        var groups = GroupFormer.Form(ToGroupingStudents(roster), request.GroupSize, request.BalanceByGender, request.Seed);

        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        var grouping = new Grouping
        {
            ClassId = classId,
            Name = name,
            GroupSize = request.GroupSize,
            BalancedByGender = request.BalanceByGender,
        };

        for (var groupNumber = 0; groupNumber < groups.Count; groupNumber++)
        {
            foreach (var studentId in groups[groupNumber])
            {
                grouping.Members.Add(new GroupMember
                {
                    GroupingId = grouping.Id,
                    StudentId = studentId,
                    GroupNumber = groupNumber,
                });
            }
        }

        db.Groupings.Add(grouping);
        await db.SaveChangesAsync();

        var response = new SavedGroupingResponse(
            grouping.Id, classId, grouping.Name, grouping.GroupSize, grouping.BalancedByGender,
            grouping.CreatedAt, ToGroupResponses(groups, roster));
        return Results.Created($"/api/classes/{classId}/groupings/{grouping.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        Guid classId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var summaries = await db.Groupings
            .Where(g => g.ClassId == classId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GroupingSummaryResponse(
                g.Id,
                g.Name,
                g.GroupSize,
                g.BalancedByGender,
                g.CreatedAt,
                g.Members.Select(m => m.GroupNumber).Distinct().Count(),
                g.Members.Count))
            .ToListAsync();

        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetAsync(
        Guid classId,
        Guid groupingId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var grouping = await db.Groupings
            .FirstOrDefaultAsync(g => g.Id == groupingId && g.ClassId == classId);
        if (grouping is null)
        {
            return Results.Problem(
                title: "Grouping not found",
                detail: "No such grouping exists in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Join the saved placements to the live roster: soft-deleted students drop out via the global
        // filter, so a removed student simply no longer appears in the retrieved grouping.
        var members =
            from m in db.GroupMembers
            join s in db.Students on m.StudentId equals s.Id
            where m.GroupingId == groupingId
            select new { m.GroupNumber, s.Id, s.DisplayName, s.Gender };

        var groups = (await members.ToListAsync())
            .GroupBy(m => m.GroupNumber)
            .OrderBy(g => g.Key)
            .Select(g => new FormedGroupResponse(
                g.Key,
                g.Select(m => new GroupMemberResponse(m.Id, m.DisplayName, m.Gender))
                    .OrderBy(m => m.DisplayName)
                    .ToList()))
            .ToList();

        return Results.Ok(new SavedGroupingResponse(
            grouping.Id, classId, grouping.Name, grouping.GroupSize, grouping.BalancedByGender,
            grouping.CreatedAt, groups));
    }

    private static async Task<bool> IsMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new ClassMembershipRequirement());
        return result.Succeeded;
    }

    /// <summary>Loads the class roster (id, name, gender) in a stable order — the basis for seedable forming.</summary>
    private static async Task<List<RosterRow>> LoadRosterAsync(ClassroomDbContext db, Guid classId) =>
        await db.Students
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new RosterRow(s.Id, s.DisplayName, s.Gender))
            .ToListAsync();

    private static IReadOnlyList<GroupingStudent> ToGroupingStudents(IEnumerable<RosterRow> roster) =>
        roster.Select(r => new GroupingStudent(r.Id, r.Gender)).ToList();

    /// <summary>Maps formed id-groups to display rows, resolving each id against the loaded roster.</summary>
    private static IReadOnlyList<FormedGroupResponse> ToGroupResponses(
        IReadOnlyList<IReadOnlyList<Guid>> groups, IReadOnlyList<RosterRow> roster)
    {
        var byId = roster.ToDictionary(r => r.Id);
        return groups
            .Select((members, index) => new FormedGroupResponse(
                index,
                members.Select(id => new GroupMemberResponse(id, byId[id].DisplayName, byId[id].Gender)).ToList()))
            .ToList();
    }

    private static IResult NoStudents() => Results.Problem(
        title: "No students to group",
        detail: "This class has no students to form groups from.",
        statusCode: StatusCodes.Status400BadRequest);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);

    private readonly record struct RosterRow(Guid Id, string DisplayName, Gender? Gender);
}
