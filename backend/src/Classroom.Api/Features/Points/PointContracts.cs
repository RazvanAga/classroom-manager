using Classroom.Domain.Points;
using FluentValidation;

namespace Classroom.Api.Features.Points;

/// <summary>
/// Award or deduct points to one student or many at once. Provide <b>either</b> a
/// <paramref name="BehaviorId"/> (uses that behavior's signed default points) <b>or</b> an explicit
/// signed <paramref name="Amount"/> with an optional reason (a manual adjustment) — not both.
/// Selecting several students writes one row per student sharing a single batch id (design.md §2.2).
/// </summary>
public record AwardPointsRequest(
    IReadOnlyList<Guid> StudentIds,
    Guid? BehaviorId,
    int? Amount,
    string? Reason);

public record PointTransactionResponse(
    Guid Id,
    Guid StudentId,
    int Amount,
    PointTransactionType Type,
    Guid? BehaviorId,
    Guid? BatchId,
    string? Reason,
    DateTime CreatedAt);

/// <summary>The rows written by one award call; <see cref="BatchId"/> is set when it targeted many students.</summary>
public record AwardResponse(Guid? BatchId, IReadOnlyList<PointTransactionResponse> Transactions);

/// <summary>A single student's two derived balances (design.md §2.1).</summary>
public record BalanceResponse(Guid StudentId, int Wallet, int LifetimeEarned);

/// <summary>One leaderboard rank: a student with their wallet and lifetime-earned totals.</summary>
public record LeaderboardEntryResponse(Guid StudentId, string DisplayName, int Wallet, int LifetimeEarned);

/// <summary>
/// One row in the recent-activity feed that drives undo. Carries the display context (student and
/// behavior names) and <see cref="VoidedAt"/> so the UI can show — and stop offering undo on — voided
/// rows. <see cref="BatchId"/> lets the UI undo a bulk award as a unit (design.md §2.4).
/// </summary>
public record TransactionListItem(
    Guid Id,
    Guid StudentId,
    string StudentName,
    int Amount,
    PointTransactionType Type,
    Guid? BehaviorId,
    string? BehaviorName,
    Guid? BatchId,
    string? Reason,
    DateTime CreatedAt,
    DateTime? VoidedAt);

/// <summary>The result of voiding a bulk award by batch id.</summary>
public record BatchVoidResponse(Guid BatchId, int VoidedCount);

public class AwardPointsRequestValidator : AbstractValidator<AwardPointsRequest>
{
    public AwardPointsRequestValidator()
    {
        RuleFor(x => x.StudentIds)
            .NotEmpty().WithMessage("Select at least one student.");

        // Exactly one source of the amount: a catalog behavior or an explicit signed amount.
        RuleFor(x => x)
            .Must(x => x.BehaviorId.HasValue ^ x.Amount.HasValue)
            .WithMessage("Provide either a behaviorId or an explicit amount, but not both.");

        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
