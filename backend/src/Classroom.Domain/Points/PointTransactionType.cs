namespace Classroom.Domain.Points;

/// <summary>
/// What a ledger row represents (design.md §2.1). Stored as a string (CLAUDE.md invariant).
/// This slice (#6) writes only <see cref="Award"/> and <see cref="Deduction"/>; <see cref="Purchase"/>
/// arrives with the store (slice #9) and <see cref="Adjustment"/> covers ad-hoc manual corrections.
/// </summary>
public enum PointTransactionType
{
    /// <summary>A positive, behavior-driven reward; the only type counted toward lifetime earned.</summary>
    Award,

    /// <summary>A behavior-driven deduction (typically negative); lowers the wallet, not lifetime.</summary>
    Deduction,

    /// <summary>A store purchase (slice #9); a negative spend against the wallet only.</summary>
    Purchase,

    /// <summary>A manual, non-behavior correction; affects the wallet but never lifetime earned.</summary>
    Adjustment,
}
