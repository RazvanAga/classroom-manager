using Classroom.Domain.Common;

namespace Classroom.Domain.Classes;

/// <summary>
/// A class a teacher manages. Many-to-many with teachers via <see cref="ClassTeacher"/> (design.md §1.2).
/// <para>
/// Two distinct lifecycle flags: <see cref="IsArchived"/> closes a class at year-end but keeps it
/// queryable (history is retained), whereas <see cref="DeletedAt"/> is a soft-delete hidden by the
/// EF global query filter (design.md §7.1 — the one place a query filter is used).
/// </para>
/// </summary>
public class Class
{
    public Guid Id { get; set; } = EntityId.New();

    public required string Name { get; set; }

    /// <summary>
    /// The reward-currency icon shown next to point totals (slice #19). Purely presentational;
    /// defaults to <see cref="CurrencyIcon.Star"/> on creation and seed. Changeable by any member.
    /// </summary>
    public CurrencyIcon CurrencyIcon { get; set; } = CurrencyIcon.Star;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Archived classes are excluded from the active list but remain in the database.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Set on soft-delete; hidden by the global query filter while non-null.</summary>
    public DateTime? DeletedAt { get; set; }

    public ICollection<ClassTeacher> Teachers { get; set; } = new List<ClassTeacher>();
}
