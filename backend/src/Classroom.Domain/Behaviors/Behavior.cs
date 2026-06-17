using Classroom.Domain.Common;

namespace Classroom.Domain.Behaviors;

/// <summary>
/// A named, awardable reason in a class's catalog (design.md §2.2), carrying a signed default point
/// value — positive to reward, negative to deduct. The catalog is <b>per-class</b>: a new class is
/// seeded by copying a code template into its own rows (design.md §2.3, <see cref="DefaultBehaviors"/>),
/// so editing one class never affects another. No shared FK to a global behavior.
/// </summary>
public class Behavior
{
    public Guid Id { get; set; } = EntityId.New();

    public required Guid ClassId { get; set; }

    public required string Name { get; set; }

    /// <summary>Signed: positive awards points, negative deducts them.</summary>
    public required int DefaultPoints { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
