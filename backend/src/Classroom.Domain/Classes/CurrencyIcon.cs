namespace Classroom.Domain.Classes;

/// <summary>
/// The reward-currency icon a class shows next to point totals. Purely presentational — points stay
/// "points" in the code and ledger; children just see an icon + number (design.md §1.2, slice #19).
/// <para>
/// A fixed, curated set (not free-form) so every screen — grid, leaderboard, shop, reports — can
/// render the same known glyphs consistently. <see cref="Star"/> is value 0 so a new class defaults
/// to it without explicit initialization. Stored as a string (CLAUDE.md invariant: enums as strings).
/// </para>
/// </summary>
public enum CurrencyIcon
{
    Star,
    Coin,
    Gem,
    Heart,
    Flower,
    JellyBean,
    Apple,
    Trophy,
    Crown,
    Rainbow,
}
