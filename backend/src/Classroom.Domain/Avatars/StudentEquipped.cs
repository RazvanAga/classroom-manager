namespace Classroom.Domain.Avatars;

/// <summary>
/// The single item a student currently has equipped in a given <see cref="AvatarSlot"/>. The pair
/// <c>(StudentId, Slot)</c> is the composite primary key, so there is at most one equipped option per
/// slot; equipping a slot upserts this row. The referenced item must be one the student owns.
/// <para>
/// New students start with the default item equipped in every slot, so they render a complete, valid
/// avatar immediately (design.md §3.3). Switching is free (design.md §3.3); the wallet is untouched.
/// </para>
/// </summary>
public class StudentEquipped
{
    public required Guid StudentId { get; set; }

    public required AvatarSlot Slot { get; set; }

    public required Guid ItemId { get; set; }
}
