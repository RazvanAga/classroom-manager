using Classroom.Domain.Reporting;

namespace Classroom.UnitTests.Reporting;

public class PointTimelineTests
{
    private static DateTime Utc(string iso) =>
        DateTime.SpecifyKind(DateTime.Parse(iso), DateTimeKind.Utc);

    [Fact]
    public void Builds_one_bucket_per_day_across_the_whole_range_including_empty_days()
    {
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 6, 3);

        var buckets = PointTimeline.Build([], from, to, offsetMinutes: 0, openingBalance: 0);

        Assert.Equal(3, buckets.Count);
        Assert.Equal(new[] { from, from.AddDays(1), to }, buckets.Select(b => b.Date));
        Assert.All(buckets, b => Assert.Equal(0, b.Net));
        Assert.All(buckets, b => Assert.Equal(0, b.Balance));
    }

    [Fact]
    public void Splits_a_days_rows_into_earned_spent_and_net()
    {
        var day = new DateOnly(2026, 6, 1);
        var points = new[]
        {
            new LedgerPoint(Utc("2026-06-01T09:00:00"), 5),
            new LedgerPoint(Utc("2026-06-01T10:00:00"), 3),
            new LedgerPoint(Utc("2026-06-01T11:00:00"), -4),
        };

        var bucket = Assert.Single(PointTimeline.Build(points, day, day, offsetMinutes: 0, openingBalance: 0));

        Assert.Equal(8, bucket.Earned);
        Assert.Equal(-4, bucket.Spent);
        Assert.Equal(4, bucket.Net);
        Assert.Equal(4, bucket.Balance);
    }

    [Fact]
    public void Running_balance_carries_the_opening_balance_forward_across_days()
    {
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 6, 3);
        var points = new[]
        {
            new LedgerPoint(Utc("2026-06-01T09:00:00"), 5),
            // nothing on the 2nd
            new LedgerPoint(Utc("2026-06-03T09:00:00"), -2),
        };

        var buckets = PointTimeline.Build(points, from, to, offsetMinutes: 0, openingBalance: 10);

        Assert.Equal(15, buckets[0].Balance); // 10 + 5
        Assert.Equal(15, buckets[1].Balance); // unchanged on the empty day
        Assert.Equal(13, buckets[2].Balance); // 15 - 2
    }

    [Fact]
    public void Buckets_by_the_teachers_local_day_not_utc()
    {
        // 23:30 UTC on the 1st is 01:30 local on the 2nd at UTC+2 — it must land on the 2nd.
        var points = new[] { new LedgerPoint(Utc("2026-06-01T23:30:00"), 7) };
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 6, 2);

        var buckets = PointTimeline.Build(points, from, to, offsetMinutes: 120, openingBalance: 0);

        Assert.Equal(0, buckets[0].Net); // the 1st (local) saw nothing
        Assert.Equal(7, buckets[1].Net); // the award belongs to the 2nd (local)
    }

    [Fact]
    public void UtcWindow_brackets_the_local_date_range_at_local_midnight()
    {
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 6, 1);

        var (fromUtc, toUtcExclusive) = PointTimeline.ToUtcWindow(from, to, offsetMinutes: 120);

        // Local midnight on the 1st (UTC+2) is 22:00 UTC on the previous day; the exclusive upper
        // bound is local midnight starting the 2nd, i.e. 22:00 UTC on the 1st.
        Assert.Equal(Utc("2026-05-31T22:00:00"), fromUtc);
        Assert.Equal(Utc("2026-06-01T22:00:00"), toUtcExclusive);
        Assert.Equal(DateTimeKind.Utc, fromUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, toUtcExclusive.Kind);
    }

    [Fact]
    public void UtcWindow_with_zero_offset_is_plain_utc_midnight_to_midnight()
    {
        var (fromUtc, toUtcExclusive) =
            PointTimeline.ToUtcWindow(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), offsetMinutes: 0);

        Assert.Equal(Utc("2026-06-01T00:00:00"), fromUtc);
        Assert.Equal(Utc("2026-06-03T00:00:00"), toUtcExclusive); // inclusive 'to' → exclusive next-midnight
    }
}
