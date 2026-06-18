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
