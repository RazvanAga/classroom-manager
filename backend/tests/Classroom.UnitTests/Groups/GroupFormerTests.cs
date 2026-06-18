using Classroom.Domain.Groups;
using Classroom.Domain.Students;

namespace Classroom.UnitTests.Groups;

public class GroupFormerTests
{
    private static IReadOnlyList<GroupingStudent> Students(int count, Gender? gender = null) =>
        Enumerable.Range(0, count).Select(_ => new GroupingStudent(Guid.NewGuid(), gender)).ToList();

    private static List<int> Sizes(IReadOnlyList<IReadOnlyList<Guid>> groups) =>
        groups.Select(g => g.Count).ToList();

    [Fact]
    public void An_empty_roster_forms_no_groups()
    {
        Assert.Empty(GroupFormer.Form([], groupSize: 3, balanceByGender: false, seed: 1));
    }

    [Fact]
    public void A_group_size_below_one_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GroupFormer.Form(Students(4), groupSize: 0, balanceByGender: false, seed: 1));
    }

    [Fact]
    public void Exact_division_yields_equal_groups()
    {
        var groups = GroupFormer.Form(Students(6), groupSize: 2, balanceByGender: false, seed: 1);

        Assert.Equal(3, groups.Count);
        Assert.All(groups, g => Assert.Equal(2, g.Count));
    }

    [Fact]
    public void A_remainder_spreads_so_groups_differ_by_at_most_one()
    {
        // 7 students, size 2 => ceil(7/2) = 4 groups => 2,2,2,1.
        var groups = GroupFormer.Form(Students(7), groupSize: 2, balanceByGender: false, seed: 1);

        Assert.Equal(4, groups.Count);
        var sizes = Sizes(groups);
        Assert.Equal(7, sizes.Sum());
        Assert.True(sizes.Max() - sizes.Min() <= 1);
    }

    [Fact]
    public void Fewer_students_than_the_group_size_forms_one_group_of_everyone()
    {
        var students = Students(3);
        var groups = GroupFormer.Form(students, groupSize: 5, balanceByGender: false, seed: 1);

        var only = Assert.Single(groups);
        Assert.Equal(3, only.Count);
    }

    [Fact]
    public void Every_student_is_placed_exactly_once()
    {
        var students = Students(10);

        var groups = GroupFormer.Form(students, groupSize: 3, balanceByGender: false, seed: 5);

        var placed = groups.SelectMany(g => g).ToList();
        Assert.Equal(students.Count, placed.Count);
        Assert.Equal(students.Select(s => s.Id).OrderBy(x => x), placed.OrderBy(x => x));
    }

    [Fact]
    public void Grouping_is_deterministic_for_a_fixed_seed()
    {
        var students = Students(11);

        var first = GroupFormer.Form(students, groupSize: 3, balanceByGender: false, seed: 99);
        var again = GroupFormer.Form(students, groupSize: 3, balanceByGender: false, seed: 99);

        Assert.Equal(Sizes(first), Sizes(again));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], again[i]);
        }
    }

    [Fact]
    public void Different_seeds_can_produce_different_arrangements()
    {
        var students = Students(12);

        var a = GroupFormer.Form(students, groupSize: 3, balanceByGender: false, seed: 1);
        var b = GroupFormer.Form(students, groupSize: 3, balanceByGender: false, seed: 2);

        // Same sizes, but at least one student lands in a different group slot.
        var differs = Enumerable.Range(0, a.Count).Any(i => !a[i].SequenceEqual(b[i]));
        Assert.True(differs);
    }

    [Fact]
    public void Gender_balancing_spreads_every_bucket_including_unrecorded_evenly()
    {
        // Distinct counts per stratum so an uneven spread would be obvious.
        var female = Students(6, Gender.Female);
        var male = Students(5, Gender.Male);
        var unrecorded = Students(4);
        var all = female.Concat(male).Concat(unrecorded).ToList();

        var groups = GroupFormer.Form(all, groupSize: 3, balanceByGender: true, seed: 7);

        var femaleIds = female.Select(s => s.Id).ToHashSet();
        var maleIds = male.Select(s => s.Id).ToHashSet();
        var unrecordedIds = unrecorded.Select(s => s.Id).ToHashSet();

        foreach (var (bucket, _) in new[]
                 {
                     (femaleIds, "Female"), (maleIds, "Male"), (unrecordedIds, "unrecorded"),
                 })
        {
            var perGroup = groups.Select(g => g.Count(bucket.Contains)).ToList();
            Assert.True(perGroup.Max() - perGroup.Min() <= 1,
                $"a gender bucket was spread unevenly: [{string.Join(",", perGroup)}]");
        }

        // Overall sizes still even.
        var sizes = Sizes(groups);
        Assert.True(sizes.Max() - sizes.Min() <= 1);
    }

    [Fact]
    public void Gender_balanced_grouping_is_also_deterministic_for_a_fixed_seed()
    {
        var all = Students(5, Gender.Female).Concat(Students(4, Gender.Male)).Concat(Students(3)).ToList();

        var first = GroupFormer.Form(all, groupSize: 4, balanceByGender: true, seed: 21);
        var again = GroupFormer.Form(all, groupSize: 4, balanceByGender: true, seed: 21);

        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], again[i]);
        }
    }
}
