namespace Classroom.Domain.Avatars;

/// <summary>
/// A customizable layer of the locked DiceBear <c>adventurer</c> style (design.md §3.1/§3.2). Each
/// slot holds exactly one equipped option at a time; a store item is one option value for one slot.
/// <para>
/// Only <b>always-on</b> option groups are modeled here — every slot is always rendered, so a student
/// always has a complete avatar. Probability-gated/optional groups (e.g. glasses, earrings) are left
/// for a later accessory slice. The enum name maps to the DiceBear option key by lower-casing the
/// first letter (<c>HairColor</c> → <c>hairColor</c>); the frontend owns composition.
/// </para>
/// </summary>
public enum AvatarSlot
{
    Hair,
    HairColor,
    SkinColor,
    Eyes,
    Mouth,
}
