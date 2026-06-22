using Classroom.Domain.Points;
using Classroom.Domain.Reporting;

namespace Classroom.Api.Features.Reporting;

/// <summary>
/// A student's point history bucketed by the teacher's local day (design.md §9.3). <see cref="OpeningBalance"/>
/// is the wallet just before <see cref="From"/>, so <see cref="DailyPointBucket.Balance"/> reads as the true
/// running wallet across the range. <see cref="Days"/> spans every day in the range (empty days included).
/// </summary>
public record StudentTimelineResponse(
    Guid StudentId,
    string DisplayName,
    DateOnly From,
    DateOnly To,
    int OpeningBalance,
    int TotalEarned,
    int TotalSpent,
    IReadOnlyList<DailyPointBucket> Days);

/// <summary>One behavior's contribution to a class breakdown over the range.</summary>
/// <param name="Count">How many non-voided rows came from this behavior.</param>
/// <param name="TotalPoints">The signed sum of those rows (positive for rewards, negative for deductions).</param>
public record BehaviorStat(Guid BehaviorId, string Name, int Count, int TotalPoints);

/// <summary>
/// A per-class behavior breakdown over a selectable local-day range (design.md §9.3): the totals plus
/// each behavior's frequency and point contribution, so the UI can surface the most common positive
/// and negative behaviors. Only behavior-driven rows count — ad-hoc adjustments and purchases are excluded.
/// </summary>
public record BehaviorBreakdownResponse(
    DateOnly From,
    DateOnly To,
    int TotalAwarded,
    int TotalDeducted,
    int NetPoints,
    int AwardCount,
    IReadOnlyList<BehaviorStat> Behaviors);

/// <summary>
/// One row of a student's transaction history (design.md §9.3): the signed <see cref="Amount"/>, its
/// <see cref="Type"/>, the behavior it came from (when behavior-driven), the free-text
/// <see cref="Reason"/> note explaining <i>why</i>, and <see cref="VoidedAt"/> so an undone row reads
/// as struck-through rather than vanishing. Unlike the timeline/breakdown aggregations, the history
/// lists every row — voided ones included — so the audit trail stays complete.
/// </summary>
public record StudentHistoryItem(
    Guid Id,
    int Amount,
    PointTransactionType Type,
    Guid? BehaviorId,
    string? BehaviorName,
    string? Reason,
    DateTime CreatedAt,
    DateTime? VoidedAt);

/// <summary>
/// A page of a student's transaction history, most-recent-first. The class-wide activity feed is
/// capped and not per-student, so this paginated endpoint backs the student profile's full history.
/// <see cref="TotalPages"/> is derived from <see cref="TotalCount"/>/<see cref="PageSize"/> so the UI
/// knows when to stop paging.
/// </summary>
public record StudentHistoryResponse(
    Guid StudentId,
    string DisplayName,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<StudentHistoryItem> Items);
