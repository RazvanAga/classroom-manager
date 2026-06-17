using Classroom.Domain.Common;

namespace Classroom.Domain.Students;

/// <summary>
/// A student belonging to exactly one class (the homeroom model, design.md §1.3): points, avatar,
/// and inventory all hang unambiguously off this student-in-this-class.
/// <para>
/// Only minimal PII is stored — a display/first name and an optional <see cref="Gender"/>; no email
/// or date of birth (design.md §7.1). Removal is a <b>soft delete</b>: <see cref="DeletedAt"/> is set
/// and the row is then hidden everywhere by the EF global query filter (the one place a query filter
/// is used), preserving ledger history and making accidental removal undoable.
/// </para>
/// </summary>
public class Student
{
    public Guid Id { get; set; } = EntityId.New();

    /// <summary>The class this student belongs to (exactly one — design.md §1.3).</summary>
    public required Guid ClassId { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Optional; left null when no marker is recorded (design.md §6.3/§7.2).</summary>
    public Gender? Gender { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set on soft-delete; hidden by the global query filter while non-null.</summary>
    public DateTime? DeletedAt { get; set; }
}
