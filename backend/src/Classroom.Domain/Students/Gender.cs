namespace Classroom.Domain.Students;

/// <summary>
/// A student's gender, used only for optional gender-balanced grouping (design.md §6.3).
/// A strict binary enum by owner decision; the field is <b>nullable</b> at the entity so it can be
/// left unset, respecting the minimal-PII stance for minors (design.md §7.1/§7.2).
/// Stored as a string in the database (CLAUDE.md invariant: enums as strings).
/// </summary>
public enum Gender
{
    Female,
    Male,
}
