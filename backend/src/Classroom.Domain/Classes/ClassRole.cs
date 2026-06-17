namespace Classroom.Domain.Classes;

/// <summary>
/// A teacher's role within a class (design.md §1.2). The creator becomes <see cref="Owner"/>;
/// Owners can perform destructive ops (delete the class, add/remove teachers) — Collaborators cannot.
/// Stored as a string in the database (CLAUDE.md invariant: enums as strings).
/// </summary>
public enum ClassRole
{
    Owner,
    Collaborator,
}
