using Classroom.Domain.Common;

namespace Classroom.UnitTests.Common;

// First occupant of the pure-domain (no-DB) unit-test seam. Later slices add the
// algorithm tests here (roster parser, ledger aggregation, fair pick, group formation).
public class EntityIdTests
{
    [Fact]
    public void New_produces_a_version_7_guid()
    {
        var id = EntityId.New();

        Assert.Equal(7, id.Version);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void New_produces_unique_ids()
    {
        var ids = Enumerable.Range(0, 1000).Select(_ => EntityId.New()).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void New_ids_are_time_ordered()
    {
        // GUID v7 embeds a millisecond timestamp, so ids minted later sort after earlier ones.
        var first = EntityId.New();
        Thread.Sleep(5);
        var second = EntityId.New();

        Assert.True(string.CompareOrdinal(first.ToString(), second.ToString()) < 0);
    }
}
