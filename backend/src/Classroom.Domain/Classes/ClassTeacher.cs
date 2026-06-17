using Classroom.Domain.Identity;

namespace Classroom.Domain.Classes;

/// <summary>
/// Join row binding a teacher to a class with a <see cref="ClassRole"/> (design.md §1.2).
/// Composite primary key <c>(ClassId, TeacherId)</c> — a teacher appears at most once per class.
/// Membership here is what the resource-based authorization handlers check (design.md §5.3).
/// </summary>
public class ClassTeacher
{
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; } = null!;

    public required ClassRole Role { get; set; }
}
