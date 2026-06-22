using System.Security.Claims;
using Classroom.Api.Common.Authorization;
using Classroom.Domain.Reporting;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Reporting;

public static class ReportingEndpoints
{
    // A class's worth of buckets is bounded by how many days a teacher would chart at once. Capping
    // the range keeps the in-memory day loop and the SQL window predictable (design.md §9.3).
    private const int DefaultRangeDays = 30;
    private const int MaxRangeDays = 366;

    // ±14h covers every real-world offset; clamp the (untrusted) client value rather than trust it.
    private const int MaxOffsetMinutes = 14 * 60;

    // Page size bounds for the per-student history — a modest default with a cap so a hostile
    // page-size can't drag the whole ledger into memory.
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        // Read-only teacher analytics, scoped to a class the teacher must be a member of. Kiosk
        // principals are rejected — this is not one of the widened kiosk endpoints (design.md §5.4).
        var group = app.MapGroup("/api/classes/{classId:guid}/reports")
            .WithTags("Reporting")
            .RequireAuthorization();

        group.MapGet("/students/{studentId:guid}/timeline", StudentTimelineAsync)
            .WithSummary("A student's point history bucketed by the teacher's local day over a date range.");

        group.MapGet("/behaviors", BehaviorBreakdownAsync)
            .WithSummary("A per-class behavior breakdown (counts + point totals) over a date range.");

        group.MapGet("/students/{studentId:guid}/history", StudentHistoryAsync)
            .WithSummary("A student's paginated point transaction history including notes (most recent first).");

        return app;
    }

    private static async Task<IResult> StudentHistoryAsync(
        Guid classId,
        Guid studentId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var student = await db.Students
            .Where(s => s.Id == studentId && s.ClassId == classId)
            .Select(s => new { s.Id, s.DisplayName })
            .FirstOrDefaultAsync();
        if (student is null)
        {
            return Results.Problem(
                title: "Student not found",
                detail: "No such student exists in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Show every row — voided included — so the profile's history is a complete audit trail; only
        // the timeline/breakdown aggregations drop voided rows. The behavior name is left-joined for
        // display, and the note (Reason) rides along so a teacher can see *why* points were given.
        var totalCount = await db.PointTransactions.CountAsync(t => t.StudentId == studentId);

        var items = await (
                from t in db.PointTransactions
                join b in db.Behaviors on t.BehaviorId equals b.Id into behaviorJoin
                from b in behaviorJoin.DefaultIfEmpty()
                where t.StudentId == studentId
                // GUID v7 ids are time-ordered, so they break ties within a bulk award deterministically.
                orderby t.CreatedAt descending, t.Id descending
                select new StudentHistoryItem(
                    t.Id,
                    t.Amount,
                    t.Type,
                    t.BehaviorId,
                    b != null ? b.Name : null,
                    t.Reason,
                    t.CreatedAt,
                    t.VoidedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Results.Ok(new StudentHistoryResponse(
            student.Id, student.DisplayName, page, pageSize, totalCount, totalPages, items));
    }

    private static async Task<IResult> StudentTimelineAsync(
        Guid classId,
        Guid studentId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz,
        DateOnly? from = null,
        DateOnly? to = null,
        int tzOffsetMinutes = 0)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var student = await db.Students
            .Where(s => s.Id == studentId && s.ClassId == classId)
            .Select(s => new { s.Id, s.DisplayName })
            .FirstOrDefaultAsync();
        if (student is null)
        {
            return Results.Problem(
                title: "Student not found",
                detail: "No such student exists in this class.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var offset = ClampOffset(tzOffsetMinutes);
        if (!TryResolveRange(from, to, offset, out var range, out var rangeError))
        {
            return rangeError;
        }

        var (fromUtc, toUtcExclusive) = PointTimeline.ToUtcWindow(range.From, range.To, offset);

        // Opening balance: the student's wallet just before the window starts (all non-voided rows),
        // so the running balance in the timeline reads as their true wallet, not a from-zero count.
        var openingBalance = await db.PointTransactions
            .Where(t => t.StudentId == studentId && t.VoidedAt == null && t.CreatedAt < fromUtc)
            .SumAsync(t => (int?)t.Amount) ?? 0;

        // Pull just the in-window, non-voided rows and fold them with the pure, unit-tested bucketer —
        // keeping PointTimeline the single source of truth for local-day grouping.
        var points = await db.PointTransactions
            .Where(t => t.StudentId == studentId && t.VoidedAt == null
                && t.CreatedAt >= fromUtc && t.CreatedAt < toUtcExclusive)
            .Select(t => new LedgerPoint(t.CreatedAt, t.Amount))
            .ToListAsync();

        var days = PointTimeline.Build(points, range.From, range.To, offset, openingBalance);
        var totalEarned = days.Sum(d => d.Earned);
        var totalSpent = days.Sum(d => d.Spent);

        return Results.Ok(new StudentTimelineResponse(
            student.Id, student.DisplayName, range.From, range.To, openingBalance,
            totalEarned, totalSpent, days));
    }

    private static async Task<IResult> BehaviorBreakdownAsync(
        Guid classId,
        ClaimsPrincipal user,
        ClassroomDbContext db,
        IAuthorizationService authz,
        DateOnly? from = null,
        DateOnly? to = null,
        int tzOffsetMinutes = 0)
    {
        if (!await IsMember(authz, user, classId))
        {
            return Forbidden();
        }

        var offset = ClampOffset(tzOffsetMinutes);
        if (!TryResolveRange(from, to, offset, out var range, out var rangeError))
        {
            return rangeError;
        }

        var (fromUtc, toUtcExclusive) = PointTimeline.ToUtcWindow(range.From, range.To, offset);

        // Aggregate per behavior in SQL (design.md §9.3): only behavior-driven, non-voided rows in the
        // window, joined to the class roster (which also excludes soft-deleted students). Adjustments
        // and purchases carry no BehaviorId, so they fall out here.
        var grouped = await (
                from t in db.PointTransactions
                join s in db.Students on t.StudentId equals s.Id
                where s.ClassId == classId && t.VoidedAt == null && t.BehaviorId != null
                    && t.CreatedAt >= fromUtc && t.CreatedAt < toUtcExclusive
                group t by t.BehaviorId!.Value into g
                select new { BehaviorId = g.Key, Count = g.Count(), TotalPoints = g.Sum(x => x.Amount) })
            .ToListAsync();

        // Resolve names in one round-trip. A behavior deleted after its rows were written still shows,
        // labelled so the totals stay honest rather than silently dropping history.
        var behaviorIds = grouped.Select(g => g.BehaviorId).ToList();
        var names = await db.Behaviors
            .Where(b => behaviorIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        var stats = grouped
            .Select(g => new BehaviorStat(
                g.BehaviorId,
                names.GetValueOrDefault(g.BehaviorId, "(removed behavior)"),
                g.Count,
                g.TotalPoints))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Name)
            .ToList();

        var totalAwarded = stats.Where(s => s.TotalPoints > 0).Sum(s => s.TotalPoints);
        var totalDeducted = stats.Where(s => s.TotalPoints < 0).Sum(s => s.TotalPoints);
        var awardCount = stats.Sum(s => s.Count);

        return Results.Ok(new BehaviorBreakdownResponse(
            range.From, range.To, totalAwarded, totalDeducted, totalAwarded + totalDeducted,
            awardCount, stats));
    }

    private static int ClampOffset(int offsetMinutes) =>
        Math.Clamp(offsetMinutes, -MaxOffsetMinutes, MaxOffsetMinutes);

    // Resolves the inclusive local-day range, defaulting to the last 30 days ending today (local),
    // and rejecting an inverted or over-long range with a 400.
    private static bool TryResolveRange(
        DateOnly? from, DateOnly? to, int offsetMinutes,
        out (DateOnly From, DateOnly To) range, out IResult error)
    {
        var localToday = PointTimeline.LocalDay(DateTime.UtcNow, offsetMinutes);
        var resolvedTo = to ?? localToday;
        var resolvedFrom = from ?? resolvedTo.AddDays(-(DefaultRangeDays - 1));

        error = Results.Empty;
        range = (resolvedFrom, resolvedTo);

        if (resolvedFrom > resolvedTo)
        {
            error = Results.Problem(
                title: "Invalid date range",
                detail: "The 'from' date must be on or before the 'to' date.",
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        if (resolvedTo.DayNumber - resolvedFrom.DayNumber + 1 > MaxRangeDays)
        {
            error = Results.Problem(
                title: "Date range too large",
                detail: $"The reporting range cannot exceed {MaxRangeDays} days.",
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
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
