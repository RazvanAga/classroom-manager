using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Api.Common.Security;
using Classroom.Api.Common.Validation;
using Classroom.Domain.Points;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Points;

public static class PointsEndpoints
{
    public static IEndpointRouteBuilder MapPointEndpoints(this IEndpointRouteBuilder app)
    {
        // All point operations target a class the teacher must be a member of.
        var group = app.MapGroup("/api/classes/{classId:guid}")
            .WithTags("Points")
            .RequireAuthorization();

        group.MapPost("/points/award", AwardAsync)
            .AddEndpointFilter<ValidationFilter<AwardPointsRequest>>()
            .AddEndpointFilter<AntiforgeryFilter>()
            .WithSummary("Award or deduct points to one or many students (bulk shares a batch id).");

        group.MapGet("/students/{studentId:guid}/balance", BalanceAsync)
            .WithSummary("Get a student's spendable wallet and lifetime-earned total.");

        group.MapGet("/leaderboard", LeaderboardAsync)
            .WithSummary("Rank the class's students by lifetime earned.");

        return app;
    }

    private static async Task<IResult> AwardAsync(
        Guid classId,
        AwardPointsRequest request,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        // Resolve the requested students, scoped to this class — this is also the cross-tenant guard:
        // ids belonging to another class (or soft-deleted students) simply won't be found.
        var requestedIds = request.StudentIds.Distinct().ToList();
        var validIds = await db.Students
            .Where(s => s.ClassId == classId && requestedIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (validIds.Count != requestedIds.Count)
        {
            return Results.Problem(
                title: "Student not found",
                detail: "One or more students do not exist in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Determine the signed amount and type: from the catalog behavior, or an explicit adjustment.
        int amount;
        PointTransactionType type;
        Guid? behaviorId = null;

        if (request.BehaviorId is { } requestedBehaviorId)
        {
            var behavior = await db.Behaviors
                .FirstOrDefaultAsync(b => b.Id == requestedBehaviorId && b.ClassId == classId);
            if (behavior is null)
            {
                return Results.Problem(
                    title: "Behavior not found",
                    detail: "No such behavior exists in this class.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            amount = behavior.DefaultPoints;
            behaviorId = behavior.Id;
            // A non-negative behavior is a reward (counts toward lifetime); a negative one is a deduction.
            type = amount >= 0 ? PointTransactionType.Award : PointTransactionType.Deduction;
        }
        else
        {
            // Explicit amount with no behavior = a manual correction. Deliberately an Adjustment, so it
            // moves the wallet but never the leaderboard — only catalog behaviors build lifetime earned.
            amount = request.Amount!.Value;
            type = PointTransactionType.Adjustment;
        }

        // A shared batch id ties a bulk award together so it can be undone as a unit (slice #7).
        // Single-student awards don't need one.
        Guid? batchId = validIds.Count > 1 ? Guid.NewGuid() : null;
        var teacherId = user.GetTeacherId();
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var transactions = validIds
            .Select(studentId => new PointTransaction
            {
                StudentId = studentId,
                Amount = amount,
                Type = type,
                BehaviorId = behaviorId,
                AwardedByTeacherId = teacherId,
                BatchId = batchId,
                Reason = reason,
            })
            .ToList();

        db.PointTransactions.AddRange(transactions);
        await db.SaveChangesAsync();

        var response = new AwardResponse(batchId, transactions.Select(ToResponse).ToList());
        return Results.Created($"/api/classes/{classId}/leaderboard", response);
    }

    private static async Task<IResult> BalanceAsync(
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

        var studentExists = await db.Students
            .AnyAsync(s => s.Id == studentId && s.ClassId == classId);
        if (!studentExists)
        {
            return Results.Problem(
                title: "Student not found",
                detail: "No such student exists in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        // Pull the lightweight rows and fold them with the pure, unit-tested aggregation function,
        // keeping LedgerMath the single source of truth for wallet vs lifetime.
        var entries = await db.PointTransactions
            .Where(t => t.StudentId == studentId)
            .Select(t => new LedgerEntry(t.Amount, t.Type, t.VoidedAt != null))
            .ToListAsync();

        var balance = LedgerMath.Summarize(entries);
        return Results.Ok(new BalanceResponse(studentId, balance.Wallet, balance.LifetimeEarned));
    }

    private static async Task<IResult> LeaderboardAsync(
        Guid classId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        // Aggregate per student in SQL. The two sums mirror LedgerMath exactly (design.md §2.1):
        // wallet = all non-voided rows; lifetime = non-voided positive Award rows. Soft-deleted
        // students are excluded by the global query filter on Students.
        var entries = await db.Students
            .Where(s => s.ClassId == classId)
            .Select(s => new LeaderboardEntryResponse(
                s.Id,
                s.DisplayName,
                db.PointTransactions
                    .Where(t => t.StudentId == s.Id && t.VoidedAt == null)
                    .Sum(t => (int?)t.Amount) ?? 0,
                db.PointTransactions
                    .Where(t => t.StudentId == s.Id && t.VoidedAt == null
                        && t.Amount > 0 && t.Type == PointTransactionType.Award)
                    .Sum(t => (int?)t.Amount) ?? 0))
            .ToListAsync();

        var ranked = entries
            .OrderByDescending(e => e.LifetimeEarned)
            .ThenBy(e => e.DisplayName)
            .ToList();

        return Results.Ok(ranked);
    }

    private static async Task<bool> IsMember(
        IAuthorizationService authz, ClaimsPrincipal user, Guid classId)
    {
        var result = await authz.AuthorizeAsync(user, classId, new ClassMembershipRequirement());
        return result.Succeeded;
    }

    private static PointTransactionResponse ToResponse(PointTransaction t) =>
        new(t.Id, t.StudentId, t.Amount, t.Type, t.BehaviorId, t.BatchId, t.Reason, t.CreatedAt);

    private static IResult Forbidden() => Results.Problem(
        title: "Forbidden",
        detail: "You do not have access to this class.",
        statusCode: StatusCodes.Status403Forbidden);
}
