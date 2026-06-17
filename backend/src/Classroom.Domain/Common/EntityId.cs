namespace Classroom.Domain.Common;

/// <summary>
/// The single place primary keys are minted. GUID v7 is non-guessable yet time-ordered, so it
/// stays index-friendly in Postgres and safe to expose in URLs (design.md §8.5). Every entity
/// that needs a generated id should call <see cref="New"/> rather than <c>Guid.NewGuid()</c>.
/// </summary>
public static class EntityId
{
    public static Guid New() => Guid.CreateVersion7();
}
