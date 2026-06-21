namespace Classroom.Domain.Behaviors;

/// <summary>
/// The curated default behavior set copied into every new class so it is awardable from second one
/// (design.md §2.3). Held in code as plain data; <see cref="CreateFor"/> materializes fresh per-class
/// rows (the template is copied, never referenced by FK), so each class owns and can edit its own.
/// </summary>
public static class DefaultBehaviors
{
    /// <summary>
    /// (Name, signed default points) — the curated Romanian default catalog (slice #19). A mix of
    /// positive rewards and negative deductions; the UI is Romanian while code stays English.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, int DefaultPoints)> Template =
    [
        ("s-a bătut cu un coleg", -3),
        ("nu și-a făcut tema", -2),
        ("a deranjat ora", -1),
        ("a răspuns bine", 1),
        ("a ajutat un coleg", 2),
        ("a rezolvat o problemă dificilă", 3),
    ];

    /// <summary>Builds a fresh, unsaved <see cref="Behavior"/> per template entry for the given class.</summary>
    public static IReadOnlyList<Behavior> CreateFor(Guid classId) =>
        Template
            .Select(t => new Behavior { ClassId = classId, Name = t.Name, DefaultPoints = t.DefaultPoints })
            .ToList();
}
