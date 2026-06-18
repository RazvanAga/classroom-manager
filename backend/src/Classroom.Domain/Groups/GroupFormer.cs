using Classroom.Domain.Students;

namespace Classroom.Domain.Groups;

/// <summary>One student reduced to what grouping needs: identity plus the optional gender stratum.</summary>
public readonly record struct GroupingStudent(Guid Id, Gender? Gender);

/// <summary>
/// Pure, seedable random group former (design.md §6.3). The teacher chooses a target <i>group size</i>;
/// students are spread across <c>ceil(N / size)</c> groups so sizes differ by at most one (no lone
/// straggler). With gender balancing on, students are stratified into {Female, Male, unrecorded}
/// buckets and dealt round-robin so every bucket — the null/unrecorded one included — spreads evenly
/// across the groups. Deterministic for a fixed seed — the unit-tested seam CLAUDE.md calls out.
/// No DB, no I/O.
/// </summary>
public static class GroupFormer
{
    /// <summary>
    /// Forms the groups. <paramref name="groupSize"/> is the desired students-per-group (≥ 1);
    /// <paramref name="balanceByGender"/> turns on stratified dealing; <paramref name="seed"/> drives
    /// the (seedable) shuffle. Returns one inner list of student ids per group, in group order.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Guid>> Form(
        IReadOnlyList<GroupingStudent> students, int groupSize, bool balanceByGender, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(groupSize, 1);

        if (students.Count == 0)
        {
            return [];
        }

        // Group COUNT is derived from the requested size: ceil(N / size). Distributing every student
        // across that many groups keeps sizes within one of each other and avoids a small straggler
        // group (design.md §6.3 rejected the "groups of N, smaller last group" mode for that reason).
        var groupCount = (students.Count + groupSize - 1) / groupSize;
        var groups = new List<Guid>[groupCount];
        for (var i = 0; i < groupCount; i++)
        {
            groups[i] = [];
        }

        var rng = new Random(seed);
        var ordered = balanceByGender
            ? StratifiedOrder(students, rng)
            : Shuffle(students.Select(s => s.Id).ToList(), rng);

        // Round-robin deal: position k lands in group k % groupCount. This alone guarantees even
        // sizes; and because each gender stratum is a contiguous run in the stratified order, each
        // stratum is itself dealt round-robin, so it spreads across the groups to within one.
        for (var k = 0; k < ordered.Count; k++)
        {
            groups[k % groupCount].Add(ordered[k]);
        }

        return groups;
    }

    /// <summary>
    /// Concatenates the three gender buckets — each independently shuffled — in a fixed order so the
    /// result stays deterministic. Round-robin dealing of this order is what spreads each bucket.
    /// </summary>
    private static List<Guid> StratifiedOrder(IReadOnlyList<GroupingStudent> students, Random rng)
    {
        List<Guid> Bucket(Gender? gender) =>
            Shuffle(students.Where(s => s.Gender == gender).Select(s => s.Id).ToList(), rng);

        var order = new List<Guid>(students.Count);
        order.AddRange(Bucket(Gender.Female));
        order.AddRange(Bucket(Gender.Male));
        order.AddRange(Bucket(null));
        return order;
    }

    /// <summary>In-place Fisher–Yates using the supplied RNG, so a fixed seed is reproducible.</summary>
    private static List<Guid> Shuffle(List<Guid> items, Random rng)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }
}
