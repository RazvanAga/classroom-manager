namespace Classroom.Api.Features.Picker;

/// <summary>
/// A fair-pick request. The picker is stateless (design.md §6.2): the client carries the
/// already-picked set for the current cycle and sends it back each turn. Ids not on the class
/// roster are ignored server-side, and a null/empty set starts a fresh cycle.
/// </summary>
public record PickStudentRequest(IReadOnlyList<Guid>? AlreadyPickedIds);

/// <summary>
/// The chosen student plus the new already-picked set to carry into the next pick. When
/// <paramref name="CycleReset"/> is true everyone had already been picked, so the cycle reset and
/// this pick begins a fresh one.
/// </summary>
public record PickStudentResponse(
    Guid PickedStudentId,
    string PickedDisplayName,
    bool CycleReset,
    IReadOnlyList<Guid> AlreadyPickedIds);
