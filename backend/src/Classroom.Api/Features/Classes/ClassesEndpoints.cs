using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Classes;
using Classroom.Domain.Identity;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Classes;

public static class ClassesEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/classes")
            .WithTags("Classes")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<ValidationFilter<CreateClassRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Create a class; the creator becomes its Owner.");

        group.MapGet("/", ListAsync)
            .WithSummary("List the classes the current teacher belongs to (active by default).");

        group.MapGet("/{id:guid}", GetAsync)
            .WithSummary("Get a single class the current teacher belongs to.");

        group.MapPost("/{id:guid}/archive", ArchiveAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Archive a class (any member); excludes it from the active list.");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Soft-delete a class (Owner only).");

        group.MapPost("/{id:guid}/teachers", AddTeacherAsync)
            .AddEndpointFilter<ValidationFilter<AddTeacherRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Add a co-teacher by email (Owner only).");

        group.MapDelete("/{id:guid}/teachers/{teacherId:guid}", RemoveTeacherAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Remove a co-teacher (Owner only).");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateClassRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db)
    {
        var teacherId = user.GetTeacherId();

        var newClass = new Class { Name = request.Name };
        newClass.Teachers.Add(new ClassTeacher { TeacherId = teacherId, Role = ClassRole.Owner });

        db.Classes.Add(newClass);
        await db.SaveChangesAsync();

        var response = ToResponse(newClass, ClassRole.Owner);
        return Results.Created($"/api/classes/{newClass.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal user,
        ClassroomDbContext db,
        bool includeArchived = false)
    {
        var teacherId = user.GetTeacherId();

        var classes = await db.Classes
            .Where(c => c.Teachers.Any(ct => ct.TeacherId == teacherId)
                && (includeArchived || !c.IsArchived))
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ClassResponse(
                c.Id,
                c.Name,
                c.CreatedAt,
                c.IsArchived,
                c.Teachers.First(ct => ct.TeacherId == teacherId).Role))
            .ToListAsync();

        return Results.Ok(classes);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsAuthorized(authz, user, id, new ClassMembershipRequirement()))
        {
            return Forbidden();
        }

        var teacherId = user.GetTeacherId();
        var response = await db.Classes
            .Where(c => c.Id == id)
            .Select(c => new ClassResponse(
                c.Id,
                c.Name,
                c.CreatedAt,
                c.IsArchived,
                c.Teachers.First(ct => ct.TeacherId == teacherId).Role))
            .FirstOrDefaultAsync();

        return response is null ? Forbidden() : Results.Ok(response);
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsAuthorized(authz, user, id, new ClassMembershipRequirement()))
        {
            return Forbidden();
        }

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == id);
        if (target is null)
        {
            return Forbidden();
        }

        target.IsArchived = true;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsAuthorized(authz, user, id, new ClassOwnerRequirement()))
        {
            return Forbidden();
        }

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == id);
        if (target is null)
        {
            return Forbidden();
        }

        // Soft-delete (design.md §7.1): hidden by the global query filter, history preserved.
        target.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> AddTeacherAsync(
        Guid id,
        AddTeacherRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authz)
    {
        if (!await IsAuthorized(authz, user, id, new ClassOwnerRequirement()))
        {
            return Forbidden();
        }

        var target = await userManager.FindByEmailAsync(request.Email);
        if (target is null)
        {
            return Results.Problem(
                title: "Teacher not found",
                detail: $"No teacher exists with email '{request.Email}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var alreadyMember = await db.ClassTeachers
            .AnyAsync(ct => ct.ClassId == id && ct.TeacherId == target.Id);
        if (alreadyMember)
        {
            return Results.Problem(
                title: "Already a member",
                detail: "That teacher already belongs to this class.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.ClassTeachers.Add(new ClassTeacher
        {
            ClassId = id,
            TeacherId = target.Id,
            Role = request.Role ?? ClassRole.Collaborator,
        });
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveTeacherAsync(
        Guid id,
        Guid teacherId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsAuthorized(authz, user, id, new ClassOwnerRequirement()))
        {
            return Forbidden();
        }

        var membership = await db.ClassTeachers
            .FirstOrDefaultAsync(ct => ct.ClassId == id && ct.TeacherId == teacherId);
        if (membership is null)
        {
            return Results.Problem(
                title: "Not a member",
                detail: "That teacher does not belong to this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Keep every class owned: refuse to remove the last Owner (design.md §1.2).
        if (membership.Role == ClassRole.Owner)
        {
            var ownerCount = await db.ClassTeachers
                .CountAsync(ct => ct.ClassId == id && ct.Role == ClassRole.Owner);
            if (ownerCount <= 1)
            {
                return Results.Problem(
                    title: "Cannot remove the last Owner",
                    detail: "Promote another teacher to Owner before removing this one.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        db.ClassTeachers.Remove(membership);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<bool> IsAuthorized(
        IAuthorizationService authz,
        ClaimsPrincipal user,
        Guid classId,
        IAuthorizationRequirement requirement)
    {
        var result = await authz.AuthorizeAsync(user, classId, requirement);
        return result.Succeeded;
    }

    private static ClassResponse ToResponse(Class entity, ClassRole role) =>
        new(entity.Id, entity.Name, entity.CreatedAt, entity.IsArchived, role);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);
}
