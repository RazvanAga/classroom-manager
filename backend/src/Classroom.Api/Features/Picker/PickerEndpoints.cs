using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Domain.Students;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Picker;

public static class PickerEndpoints
{
    public static IEndpointRouteBuilder MapPickerEndpoints(this IEndpointRouteBuilder app)
    {
        // A teacher tool, scoped to a class the teacher must be a member of. Kiosk principals are
        // rejected (this is not one of the widened roster-read/shop/equip endpoints, design.md §5.4).
        var group = app.MapGroup("/api/classes/{classId:guid}/picker")
            .WithTags("Picker")
            .RequireAuthorization();

        group.MapPost("/pick", PickAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Pick a random student fairly — no repeats until everyone has been picked, then reset.");

        return app;
    }

    private static async Task<IResult> PickAsync(
        Guid classId,
        PickStudentRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        // The roster is server-authoritative: the eligible set and the picked student's name come
        // from the DB, not from the (untrusted) client-supplied already-picked list.
        var roster = await db.Students
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.Id, s.DisplayName })
            .ToListAsync();

        if (roster.Count == 0)
        {
            return Results.Problem(
                title: "No students to pick",
                detail: "This class has no students to pick from.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var eligible = roster.Select(s => s.Id).ToList();
        var alreadyPicked = request.AlreadyPickedIds ?? [];

        // The fairness rule lives in the pure, unit-tested domain function; the seed is fresh per
        // request so production picks are random while the function stays deterministic for tests.
        var result = FairPicker.Pick(eligible, alreadyPicked, Random.Shared.Next())!.Value;

        var name = roster.First(s => s.Id == result.PickedId).DisplayName;
        return Results.Ok(new PickStudentResponse(
            result.PickedId, name, result.CycleReset, result.AlreadyPicked));
    }

    private static async Task<bool> IsMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new ClassMembershipRequirement());
        return result.Succeeded;
    }

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);
}
