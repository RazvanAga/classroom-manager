using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Students;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Roster;

public static class RosterEndpoints
{
    public static IEndpointRouteBuilder MapRosterEndpoints(this IEndpointRouteBuilder app)
    {
        // Nested under the class: every operation targets a class the teacher must be a member of.
        var group = app.MapGroup("/api/classes/{classId:guid}/students")
            .WithTags("Roster")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithSummary("List the (non-deleted) students in a class.");

        group.MapPost("/", AddAsync)
            .AddEndpointFilter<ValidationFilter<AddStudentRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Add a single student to a class.");

        group.MapPost("/bulk", BulkAddAsync)
            .AddEndpointFilter<ValidationFilter<BulkAddStudentsRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Add students by pasting a multi-line list (Name or Name, F/M).");

        group.MapDelete("/{studentId:guid}", RemoveAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Soft-delete a student (history preserved, hidden by the global filter).");

        return app;
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

        var students = await db.Students
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new StudentResponse(s.Id, s.ClassId, s.DisplayName, s.Gender, s.CreatedAt))
            .ToListAsync();

        return Results.Ok(students);
    }

    private static async Task<IResult> AddAsync(
        Guid classId,
        AddStudentRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var student = new Student
        {
            ClassId = classId,
            DisplayName = request.DisplayName.Trim(),
            Gender = request.Gender,
        };

        db.Students.Add(student);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/classes/{classId}/students/{student.Id}",
            ToResponse(student));
    }

    private static async Task<IResult> BulkAddAsync(
        Guid classId,
        BulkAddStudentsRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        // Parsing is a pure domain function (unit-tested separately); the endpoint just persists.
        var students = RosterParser.Parse(request.Text)
            .Select(entry => new Student
            {
                ClassId = classId,
                DisplayName = entry.Name,
                Gender = entry.Gender,
            })
            .ToList();

        db.Students.AddRange(students);
        await db.SaveChangesAsync();

        var responses = students.Select(ToResponse).ToList();
        return Results.Ok(responses);
    }

    private static async Task<IResult> RemoveAsync(
        Guid classId,
        Guid studentId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId && s.ClassId == classId);
        if (student is null)
        {
            return Results.Problem(
                title: "Student not found",
                detail: "No such student exists in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Soft-delete (design.md §7.1): hidden by the global query filter, history preserved.
        student.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<bool> IsMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new ClassMembershipRequirement());
        return result.Succeeded;
    }

    private static StudentResponse ToResponse(Student s) =>
        new(s.Id, s.ClassId, s.DisplayName, s.Gender, s.CreatedAt);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);
}
