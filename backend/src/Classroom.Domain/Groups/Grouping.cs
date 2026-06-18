using Classroom.Domain.Common;

namespace Classroom.Domain.Groups;

/// <summary>
/// A saved grouping for a class — the formation a teacher liked enough to keep and reuse during an
/// activity (design.md §6.3, "optional persist"). The arrangement is materialized as
/// <see cref="GroupMember"/> rows rather than re-derived, so it survives roster changes after saving.
/// </summary>
public class Grouping
{
    public Guid Id { get; set; } = EntityId.New();

    /// <summary>The class this grouping belongs to.</summary>
    public required Guid ClassId { get; set; }

    /// <summary>Optional teacher-supplied label (e.g. "Lab pairs"); null when unnamed.</summary>
    public string? Name { get; set; }

    /// <summary>The students-per-group the teacher chose, kept for display/reference.</summary>
    public required int GroupSize { get; set; }

    /// <summary>Whether gender balancing was applied, kept for display/reference.</summary>
    public required bool BalancedByGender { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}

/// <summary>One student's placement within a saved <see cref="Grouping"/>.</summary>
public class GroupMember
{
    public Guid Id { get; set; } = EntityId.New();

    public required Guid GroupingId { get; set; }

    public required Guid StudentId { get; set; }

    /// <summary>The zero-based index of the group this student was placed in.</summary>
    public required int GroupNumber { get; set; }
}
