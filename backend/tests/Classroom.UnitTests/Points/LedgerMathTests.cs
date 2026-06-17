using Classroom.Domain.Points;

namespace Classroom.UnitTests.Points;

public class LedgerMathTests
{
    private static LedgerEntry Award(int amount, bool voided = false) =>
        new(amount, PointTransactionType.Award, voided);

    private static LedgerEntry Deduction(int amount, bool voided = false) =>
        new(amount, PointTransactionType.Deduction, voided);

    private static LedgerEntry Adjustment(int amount, bool voided = false) =>
        new(amount, PointTransactionType.Adjustment, voided);

    [Fact]
    public void Empty_ledger_is_zero_on_both_views()
    {
        var balance = LedgerMath.Summarize([]);

        Assert.Equal(0, balance.Wallet);
        Assert.Equal(0, balance.LifetimeEarned);
    }

    [Fact]
    public void Wallet_is_the_net_of_awards_and_deductions_lifetime_only_positive_awards()
    {
        var balance = LedgerMath.Summarize([Award(5), Award(3), Deduction(-4)]);

        Assert.Equal(4, balance.Wallet); // 5 + 3 - 4
        Assert.Equal(8, balance.LifetimeEarned); // 5 + 3, deduction excluded
    }

    [Fact]
    public void Deductions_can_drive_the_wallet_negative_without_touching_lifetime()
    {
        var balance = LedgerMath.Summarize([Award(2), Deduction(-10)]);

        Assert.Equal(-8, balance.Wallet);
        Assert.Equal(2, balance.LifetimeEarned);
    }

    [Fact]
    public void Voided_rows_are_excluded_from_both_views()
    {
        var balance = LedgerMath.Summarize(
        [
            Award(5),
            Award(7, voided: true), // voided: counts toward neither
            Deduction(-2, voided: true),
        ]);

        Assert.Equal(5, balance.Wallet);
        Assert.Equal(5, balance.LifetimeEarned);
    }

    [Fact]
    public void Positive_non_award_rows_count_toward_wallet_but_not_lifetime()
    {
        // A manual positive Adjustment (or a future Purchase refund) moves the wallet but must never
        // inflate the leaderboard — only behavior Awards build lifetime earned (design.md §2.1).
        var balance = LedgerMath.Summarize([Adjustment(10), Award(4)]);

        Assert.Equal(14, balance.Wallet);
        Assert.Equal(4, balance.LifetimeEarned);
    }

    [Fact]
    public void A_zero_amount_award_adds_to_neither_view()
    {
        var balance = LedgerMath.Summarize([Award(0), Award(3)]);

        Assert.Equal(3, balance.Wallet);
        Assert.Equal(3, balance.LifetimeEarned); // the 0 contributes nothing (amount > 0 required)
    }
}
