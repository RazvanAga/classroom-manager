using Classroom.Domain.Students;

namespace Classroom.UnitTests.Students;

public class FairPickerTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-0000000000c3");
    private static readonly Guid D = Guid.Parse("00000000-0000-0000-0000-0000000000d4");

    /// <summary>Drives the picker through a whole cycle, feeding each result's set into the next call.</summary>
    private static List<FairPickResult> PickSequence(IReadOnlyList<Guid> roster, int count, int seed)
    {
        var results = new List<FairPickResult>();
        IReadOnlyList<Guid> picked = [];
        for (var i = 0; i < count; i++)
        {
            var result = FairPicker.Pick(roster, picked, seed)!.Value;
            results.Add(result);
            picked = result.AlreadyPicked;
        }

        return results;
    }

    [Fact]
    public void Empty_roster_yields_no_pick()
    {
        Assert.Null(FairPicker.Pick([], [], seed: 1));
    }

    [Fact]
    public void A_pick_is_deterministic_for_a_fixed_seed()
    {
        var roster = new[] { A, B, C, D };

        var first = FairPicker.Pick(roster, [], seed: 42)!.Value;
        var again = FairPicker.Pick(roster, [], seed: 42)!.Value;

        Assert.Equal(first.PickedId, again.PickedId);
    }

    [Fact]
    public void No_student_repeats_until_everyone_has_been_picked_once()
    {
        var roster = new[] { A, B, C, D };

        var cycle = PickSequence(roster, roster.Length, seed: 7);

        var picks = cycle.Select(r => r.PickedId).ToList();
        // A full cycle is exactly the roster with no repeats — a permutation of everyone.
        Assert.Equal(roster.Length, picks.Distinct().Count());
        Assert.Equal(roster.OrderBy(x => x), picks.OrderBy(x => x));
        // No reset happens until the cycle is exhausted.
        Assert.All(cycle, r => Assert.False(r.CycleReset));
    }

    [Fact]
    public void The_cycle_resets_after_everyone_has_been_picked()
    {
        var roster = new[] { A, B, C, D };

        // Pick the whole roster, then one more.
        var afterFullCycle = PickSequence(roster, roster.Length + 1, seed: 7);

        var resetPick = afterFullCycle[^1];
        Assert.True(resetPick.CycleReset);
        // The reset pick starts a fresh cycle holding only itself.
        Assert.Equal([resetPick.PickedId], resetPick.AlreadyPicked);
    }

    [Fact]
    public void A_single_student_roster_always_picks_that_student_and_resets_each_time()
    {
        var roster = new[] { A };

        var first = FairPicker.Pick(roster, [], seed: 1)!.Value;
        Assert.Equal(A, first.PickedId);
        Assert.False(first.CycleReset);

        var second = FairPicker.Pick(roster, first.AlreadyPicked, seed: 1)!.Value;
        Assert.Equal(A, second.PickedId);
        Assert.True(second.CycleReset);
    }

    [Fact]
    public void Stale_already_picked_ids_are_dropped_and_do_not_strand_the_cycle()
    {
        var roster = new[] { A, B };
        // C and D were picked before they left the roster; the carried set must shed them.
        var stale = new[] { C, D, A };

        var result = FairPicker.Pick(roster, stale, seed: 3)!.Value;

        // Only A remained eligible-and-picked, so B is the only unpicked student — it must be chosen.
        Assert.Equal(B, result.PickedId);
        Assert.False(result.CycleReset);
        Assert.Equal([A, B], result.AlreadyPicked);
    }

    [Fact]
    public void A_newly_added_student_joins_the_current_cycle()
    {
        var roster = new[] { A, B, C };

        // A and B already picked this cycle; C was added after and has not been picked yet.
        var result = FairPicker.Pick(roster, [A, B], seed: 99)!.Value;

        Assert.Equal(C, result.PickedId);
        Assert.False(result.CycleReset);
    }
}
