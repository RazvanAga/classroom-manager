namespace Classroom.Domain.Behaviors;

/// <summary>
/// The curated default behavior set copied into every new class so it is awardable from second one
/// (design.md §2.3). Held in code as plain data; <see cref="CreateFor"/> materializes fresh per-class
/// rows (the template is copied, never referenced by FK), so each class owns and can edit its own.
/// </summary>
public static class DefaultBehaviors
{
    /// <summary>(Name, signed default points) — a mix of positive rewards and negative deductions.</summary>
    public static readonly IReadOnlyList<(string Name, int DefaultPoints)> Template =
    [
        ("Helped a peer", 2),
        ("Great participation", 1),
        ("On task", 1),
        ("Teamwork", 1),
        ("Off task", -1),
        ("Disrupting class", -2),
    ];

    /// <summary>Builds a fresh, unsaved <see cref="Behavior"/> per template entry for the given class.</summary>
    public static IReadOnlyList<Behavior> CreateFor(Guid classId) =>
        Template
            .Select(t => new Behavior { ClassId = classId, Name = t.Name, DefaultPoints = t.DefaultPoints })
            .ToList();
}
