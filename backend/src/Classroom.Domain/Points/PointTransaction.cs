using Classroom.Domain.Common;

namespace Classroom.Domain.Points;

/// <summary>
/// One immutable row in a student's point ledger (design.md §2.1). Every award, deduction, purchase,
/// or adjustment is appended here — never updated for value, never deleted. Two derived views both
/// come from summing these rows: the spendable <b>wallet</b> and <b>lifetime earned</b> (the
/// leaderboard score) — see <see cref="LedgerMath"/>.
/// <para>
/// Undo is a <b>soft-void</b> (design.md §2.4): <see cref="VoidedAt"/>/<see cref="VoidedByTeacherId"/>
/// are set and every aggregation excludes voided rows. The columns exist now; the void endpoint is
/// slice #7. A bulk award shares one <see cref="BatchId"/> across its rows so it can be undone as a unit.
/// </para>
/// </summary>
public class PointTransaction
{
    public Guid Id { get; set; } = EntityId.New();

    /// <summary>The student this row credits or debits.</summary>
    public required Guid StudentId { get; set; }

    /// <summary>Signed: positive credits the wallet, negative debits it.</summary>
    public required int Amount { get; set; }

    public required PointTransactionType Type { get; set; }

    /// <summary>The catalog behavior this row came from, when behavior-driven; null for ad-hoc adjustments.</summary>
    public Guid? BehaviorId { get; set; }

    /// <summary>The teacher who recorded this row; null for system-generated rows (e.g. purchases).</summary>
    public Guid? AwardedByTeacherId { get; set; }

    /// <summary>The store item, set only on <see cref="PointTransactionType.Purchase"/> rows (slice #9).</summary>
    public Guid? ItemId { get; set; }

    /// <summary>Shared across the rows of one bulk award so they undo together (design.md §2.2/§2.4).</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Optional free-text note, used mainly for ad-hoc adjustments.</summary>
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the row is soft-voided; while non-null the row is excluded from every aggregation.</summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>The teacher who voided the row (slice #7).</summary>
    public Guid? VoidedByTeacherId { get; set; }
}
