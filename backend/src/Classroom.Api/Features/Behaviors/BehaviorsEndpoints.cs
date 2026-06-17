using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Behaviors;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Behaviors;

public static class BehaviorsEndpoints
{
    public static IEndpointRouteBuilder MapBehaviorEndpoints(this IEndpointRouteBuilder app)
    {
        // Per-class catalog; every operation targets a class the teacher must be a member of.
        var group = app.MapGroup("/api/classes/{classId:guid}/behaviors")
            .WithTags("Behaviors")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithSummary("List a class's behavior catalog.");

        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<ValidationFilter<CreateBehaviorRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Add a behavior to the catalog.");

        group.MapPut("/{behaviorId:guid}", UpdateAsync)
            .AddEndpointFilter<ValidationFilter<UpdateBehaviorRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Edit a behavior's name or default points.");

        group.MapDelete("/{behaviorId:guid}", DeleteAsync)
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Remove a behavior from the catalog.");

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

        var behaviors = await db.Behaviors
            .Where(b => b.ClassId == classId)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new BehaviorResponse(b.Id, b.ClassId, b.Name, b.DefaultPoints, b.CreatedAt))
            .ToListAsync();

        return Results.Ok(behaviors);
    }

    private static async Task<IResult> CreateAsync(
        Guid classId,
        CreateBehaviorRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var behavior = new Behavior
        {
            ClassId = classId,
            Name = request.Name.Trim(),
            DefaultPoints = request.DefaultPoints,
        };

        db.Behaviors.Add(behavior);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/classes/{classId}/behaviors/{behavior.Id}",
            ToResponse(behavior));
    }

    private static async Task<IResult> UpdateAsync(
        Guid classId,
        Guid behaviorId,
        UpdateBehaviorRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var behavior = await db.Behaviors
            .FirstOrDefaultAsync(b => b.Id == behaviorId && b.ClassId == classId);
        if (behavior is null)
        {
            return NotFound();
        }

        behavior.Name = request.Name.Trim();
        behavior.DefaultPoints = request.DefaultPoints;
        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(behavior));
    }

    private static async Task<IResult> DeleteAsync(
        Guid classId,
        Guid behaviorId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var behavior = await db.Behaviors
            .FirstOrDefaultAsync(b => b.Id == behaviorId && b.ClassId == classId);
        if (behavior is null)
        {
            return NotFound();
        }

        // Hard delete: behaviors are not in the soft-delete set (PRD schema). The ledger slice (#6)
        // will reference behaviors with a nullable FK, so removing one never erases point history.
        db.Behaviors.Remove(behavior);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<bool> IsMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new ClassMembershipRequirement());
        return result.Succeeded;
    }

    private static BehaviorResponse ToResponse(Behavior b) =>
        new(b.Id, b.ClassId, b.Name, b.DefaultPoints, b.CreatedAt);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);

    private static IResult NotFound() => Results.Problem(
        title: "Behavior not found",
        detail: "No such behavior exists in this class.",
        statusCode: StatusCodes.Status404NotFound);
}
