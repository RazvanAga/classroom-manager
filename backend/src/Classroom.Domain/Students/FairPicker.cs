namespace Classroom.Domain.Students;

/// <summary>
/// Outcome of one fair pick: the chosen student, whether the no-repeat cycle had to reset to make
/// this pick, and the already-picked set the caller should carry into the next pick.
/// </summary>
/// <param name="PickedId">The student chosen this turn.</param>
/// <param name="CycleReset">
/// True when everyone had already been picked, so the cycle reset and this pick starts a fresh one.
/// </param>
/// <param name="AlreadyPicked">
/// The set to feed into the next <see cref="FairPicker.Pick"/> call — the previous set (minus anyone
/// no longer eligible) plus this pick, or just this pick when the cycle reset.
/// </param>
public readonly record struct FairPickResult(Guid PickedId, bool CycleReset, IReadOnlyList<Guid> AlreadyPicked);

/// <summary>
/// Pure, seedable fair student picker (design.md §6.2). "Fair" means no student is picked twice until
/// everyone eligible has been picked once, then the cycle resets. The picker is <b>stateless</b>: the
/// already-picked set is passed in and a new one is returned, so no DB table is needed (design.md
/// §6.2 rejected the persisted-log alternative). Deterministic for a fixed seed — the unit-tested
/// seam CLAUDE.md calls out. No DB, no I/O.
/// </summary>
public static class FairPicker
{
    /// <summary>
    /// Picks the next student fairly. <paramref name="eligible"/> is the class roster; its order is
    /// the tie-break basis, so a fixed <paramref name="seed"/> reproduces the same pick.
    /// <paramref name="alreadyPicked"/> is who has been picked so far this cycle (ids no longer on the
    /// roster are ignored, so removing a student never strands the cycle). Returns <c>null</c> when
    /// there are no eligible students.
    /// </summary>
    public static FairPickResult? Pick(
        IReadOnlyList<Guid> eligible, IReadOnlyCollection<Guid> alreadyPicked, int seed)
    {
        if (eligible.Count == 0)
        {
            return null;
        }

        var picked = alreadyPicked as ISet<Guid> ?? new HashSet<Guid>(alreadyPicked);

        // Everyone eligible already picked => the cycle is complete, so reset and pick from the full
        // roster again. Otherwise pick only from those not yet picked this cycle.
        var unpicked = eligible.Where(id => !picked.Contains(id)).ToList();
        var cycleReset = unpicked.Count == 0;
        var pool = cycleReset ? eligible : unpicked;

        var choice = pool[new Random(seed).Next(pool.Count)];

        // Carry forward only ids still on the roster, in roster order, plus this pick. On reset the
        // new cycle starts from just this pick.
        var carried = cycleReset
            ? [choice]
            : eligible.Where(id => picked.Contains(id) || id == choice).ToList();

        return new FairPickResult(choice, cycleReset, carried);
    }
}
