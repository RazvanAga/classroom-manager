using Classroom.Domain.Common;

namespace Classroom.Domain.Avatars;

/// <summary>
/// One option value for one <see cref="AvatarSlot"/> in the <b>global</b> catalog (design.md §3.3) —
/// there is exactly one shop for everyone, so this carries no <c>ClassId</c> (contrast the per-class
/// behavior catalog). The catalog is seeded once at startup from <see cref="AvatarCatalog"/>.
/// <para>
/// <see cref="OptionValue"/> is the literal DiceBear value for the slot (e.g. <c>short01</c>,
/// <c>562306</c>) — the frontend composes the SVG from it; no render metadata is stored server-side.
/// <see cref="IsDefault"/> marks the free starter option granted to new students (exactly one per
/// slot). <see cref="Cost"/> is 0 for now; the store and pricing arrive in slice #9.
/// </para>
/// </summary>
public class AvatarItem
{
    public Guid Id { get; set; } = EntityId.New();

    public required AvatarSlot Slot { get; set; }

    /// <summary>The literal DiceBear option value for the slot (unique within the slot).</summary>
    public required string OptionValue { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Spendable-point price; 0 in this slice (the store/economy is slice #9).</summary>
    public int Cost { get; set; }

    /// <summary>Optional cosmetic flair for the store UI; does not affect equip rules.</summary>
    public AvatarRarity? Rarity { get; set; }

    /// <summary>The free starter option for its slot, auto-granted to new students. One per slot.</summary>
    public bool IsDefault { get; set; }
}
