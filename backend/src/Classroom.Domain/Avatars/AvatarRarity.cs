namespace Classroom.Domain.Avatars;

/// <summary>
/// Optional cosmetic flair on a catalog option (PRD: <c>Rarity?</c>). Purely a label for the store
/// UI — it does not affect equip rules. Stored as a string; null is allowed for plain options.
/// </summary>
public enum AvatarRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
}
