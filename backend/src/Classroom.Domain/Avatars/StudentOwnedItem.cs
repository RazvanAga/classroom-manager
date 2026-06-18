namespace Classroom.Domain.Avatars;

/// <summary>
/// A student's ownership of one catalog <see cref="AvatarItem"/> — the inventory join. New students own
/// every default item (granted on creation, design.md §3.3); buying adds more in slice #9.
/// <para>
/// The pair <c>(StudentId, ItemId)</c> is the composite primary key, which is also the
/// <c>UNIQUE(StudentId, ItemId)</c> backstop the transactional purchase relies on against double-buy
/// (design.md §4.1). Equipping is only ever allowed for an item the student owns.
/// </para>
/// </summary>
public class StudentOwnedItem
{
    public required Guid StudentId { get; set; }

    public required Guid ItemId { get; set; }

    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
