namespace Classroom.Domain.Points;

/// <summary>The two derived views of a student's ledger (design.md §2.1).</summary>
/// <param name="Wallet">Spendable balance: the net of every non-voided row.</param>
/// <param name="LifetimeEarned">Leaderboard score: positive awards only, never reduced by spending.</param>
public readonly record struct LedgerBalance(int Wallet, int LifetimeEarned);

/// <summary>
/// One ledger row reduced to just what the aggregation needs. Keeping the math over this tiny struct
/// (rather than the EF entity) makes it a pure, DB-free, unit-testable function — the seam CLAUDE.md
/// and the PRD call out for "ledger aggregation rules: wallet vs lifetime, void exclusion."
/// </summary>
public readonly record struct LedgerEntry(int Amount, PointTransactionType Type, bool IsVoided);

/// <summary>
/// The single source of truth for turning ledger rows into balances (design.md §2.1). Both views
/// derive from the same rows, so shopping (a wallet debit) never dents a student's leaderboard standing.
/// </summary>
public static class LedgerMath
{
    /// <summary>
    /// Computes both views in one pass over the rows. Voided rows are excluded from <b>both</b>
    /// (design.md §2.4); lifetime counts only positive <see cref="PointTransactionType.Award"/> rows.
    /// </summary>
    public static LedgerBalance Summarize(IEnumerable<LedgerEntry> entries)
    {
        var wallet = 0;
        var lifetime = 0;

        foreach (var entry in entries)
        {
            if (entry.IsVoided)
            {
                continue;
            }

            wallet += entry.Amount;

            if (entry.Type == PointTransactionType.Award && entry.Amount > 0)
            {
                lifetime += entry.Amount;
            }
        }

        return new LedgerBalance(wallet, lifetime);
    }
}
