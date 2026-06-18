namespace Classroom.Domain.Reporting;

/// <summary>
/// One ledger row reduced to just what the timeline needs: when it happened (UTC) and its signed
/// amount. Keeping the bucketing over this tiny struct (not the EF entity) makes it a pure, DB-free,
/// unit-testable function — the same seam <see cref="Classroom.Domain.Points.LedgerMath"/> uses.
/// </summary>
public readonly record struct LedgerPoint(DateTime CreatedAtUtc, int Amount);

/// <summary>
/// A single local day in a student's point timeline (design.md §9.3). <paramref name="Earned"/> sums
/// the day's positive rows, <paramref name="Spent"/> its negative rows (a non-positive number),
/// <paramref name="Net"/> is their sum, and <paramref name="Balance"/> is the running wallet at the
/// end of that day (opening balance plus every net up to and including it).
/// </summary>
public readonly record struct DailyPointBucket(DateOnly Date, int Earned, int Spent, int Net, int Balance);

/// <summary>
/// Turns a student's ledger rows into a per-local-day timeline (design.md §9.3). Pure and DB-free so
/// it can be unit-tested without Postgres; the caller pre-filters voided rows and the date window in
/// SQL, then hands the survivors here. Timestamps are UTC and bucketed by the teacher's <b>local</b>
/// day via <paramref name="offsetMinutes"/> (minutes to add to UTC to reach local time), so a late-
/// evening award lands on the right calendar day for the teacher.
/// </summary>
public static class PointTimeline
{
    /// <summary>
    /// The UTC half-open instant window <c>[FromUtc, ToUtcExclusive)</c> covering the inclusive local
    /// date range <paramref name="from"/>..<paramref name="to"/>. Used to filter the ledger in SQL
    /// before bucketing, so the window boundaries honour the teacher's local midnight.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) ToUtcWindow(
        DateOnly from, DateOnly to, int offsetMinutes)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var fromUtc = from.ToDateTime(TimeOnly.MinValue) - offset;
        var toUtcExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue) - offset;
        return (
            DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(toUtcExclusive, DateTimeKind.Utc));
    }

    /// <summary>The teacher-local calendar day a UTC timestamp falls on.</summary>
    public static DateOnly LocalDay(DateTime utc, int offsetMinutes) =>
        DateOnly.FromDateTime(utc + TimeSpan.FromMinutes(offsetMinutes));

    /// <summary>
    /// Builds one bucket per local day across the whole inclusive range — days with no activity are
    /// included as zeros so a chart has no gaps — carrying the running wallet forward from
    /// <paramref name="openingBalance"/> (the student's wallet just before <paramref name="from"/>).
    /// </summary>
    public static IReadOnlyList<DailyPointBucket> Build(
        IEnumerable<LedgerPoint> points,
        DateOnly from,
        DateOnly to,
        int offsetMinutes,
        int openingBalance)
    {
        var earned = new Dictionary<DateOnly, int>();
        var spent = new Dictionary<DateOnly, int>();

        foreach (var point in points)
        {
            var day = LocalDay(point.CreatedAtUtc, offsetMinutes);
            if (point.Amount >= 0)
            {
                earned[day] = earned.GetValueOrDefault(day) + point.Amount;
            }
            else
            {
                spent[day] = spent.GetValueOrDefault(day) + point.Amount;
            }
        }

        var buckets = new List<DailyPointBucket>();
        var balance = openingBalance;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var dayEarned = earned.GetValueOrDefault(day);
            var daySpent = spent.GetValueOrDefault(day);
            var net = dayEarned + daySpent;
            balance += net;
            buckets.Add(new DailyPointBucket(day, dayEarned, daySpent, net, balance));
        }

        return buckets;
    }
}
