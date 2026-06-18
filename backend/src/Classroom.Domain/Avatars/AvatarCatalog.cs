namespace Classroom.Domain.Avatars;

/// <summary>
/// The curated, in-code source of truth for the <b>global</b> avatar catalog (design.md §3.3). The
/// .NET backend cannot run DiceBear, so the catalog is a hand-curated list of valid option values for
/// the locked <c>adventurer</c> style — each entry's <see cref="Entry.OptionValue"/> is taken verbatim
/// from that style's schema, so the frontend can render it.
/// <para>
/// The catalog is seeded from this template <b>once</b> at startup (idempotently) — not per class
/// (contrast <c>DefaultBehaviors</c>). Every slot has exactly one <see cref="Entry.IsDefault"/> option,
/// the free starter granted to new students so no one is ever faceless. Costs are 0 in this slice; the
/// store and pricing arrive in slice #9.
/// </para>
/// </summary>
public static class AvatarCatalog
{
    /// <summary>The DiceBear style this whole catalog is locked to (design.md §3.2). No cross-style mixing.</summary>
    public const string Style = "adventurer";

    /// <summary>One catalog row: a single option value for a single slot.</summary>
    public readonly record struct Entry(
        AvatarSlot Slot,
        string OptionValue,
        string DisplayName,
        bool IsDefault,
        AvatarRarity? Rarity);

    /// <summary>
    /// The full catalog. Values are literal <c>adventurer</c> schema options (variant ids for shapes,
    /// 6-hex strings for colors). Exactly one default per slot (guarded by a unit test).
    /// </summary>
    public static readonly IReadOnlyList<Entry> Template =
    [
        // Hair shape — short/long variants from the adventurer schema.
        new(AvatarSlot.Hair, "short01", "Short Hair 1", IsDefault: true, AvatarRarity.Common),
        new(AvatarSlot.Hair, "short02", "Short Hair 2", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Hair, "short03", "Short Hair 3", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Hair, "long01", "Long Hair 1", IsDefault: false, AvatarRarity.Rare),
        new(AvatarSlot.Hair, "long02", "Long Hair 2", IsDefault: false, AvatarRarity.Rare),

        // Hair color — 6-hex values from the adventurer palette.
        new(AvatarSlot.HairColor, "562306", "Brown", IsDefault: true, AvatarRarity.Common),
        new(AvatarSlot.HairColor, "0e0e0e", "Black", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.HairColor, "ac6511", "Auburn", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.HairColor, "e5d7a3", "Blonde", IsDefault: false, AvatarRarity.Rare),
        new(AvatarSlot.HairColor, "afafaf", "Silver", IsDefault: false, AvatarRarity.Rare),
        new(AvatarSlot.HairColor, "592454", "Purple", IsDefault: false, AvatarRarity.Epic),

        // Skin tone — 6-hex values, fairest to deepest.
        new(AvatarSlot.SkinColor, "ecad80", "Light", IsDefault: true, AvatarRarity.Common),
        new(AvatarSlot.SkinColor, "f2d3b1", "Fair", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.SkinColor, "9e5622", "Tan", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.SkinColor, "763900", "Deep", IsDefault: false, AvatarRarity.Common),

        // Eyes — variant ids.
        new(AvatarSlot.Eyes, "variant01", "Eyes 1", IsDefault: true, AvatarRarity.Common),
        new(AvatarSlot.Eyes, "variant02", "Eyes 2", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Eyes, "variant03", "Eyes 3", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Eyes, "variant04", "Eyes 4", IsDefault: false, AvatarRarity.Rare),
        new(AvatarSlot.Eyes, "variant05", "Eyes 5", IsDefault: false, AvatarRarity.Rare),

        // Mouth — variant ids.
        new(AvatarSlot.Mouth, "variant01", "Mouth 1", IsDefault: true, AvatarRarity.Common),
        new(AvatarSlot.Mouth, "variant02", "Mouth 2", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Mouth, "variant03", "Mouth 3", IsDefault: false, AvatarRarity.Common),
        new(AvatarSlot.Mouth, "variant04", "Mouth 4", IsDefault: false, AvatarRarity.Rare),
        new(AvatarSlot.Mouth, "variant05", "Mouth 5", IsDefault: false, AvatarRarity.Rare),
    ];

    /// <summary>The default <see cref="Entry"/> for each slot — the free starter set a new student gets.</summary>
    public static IReadOnlyList<Entry> Defaults() =>
        Template.Where(e => e.IsDefault).ToList();

    /// <summary>Builds fresh, unsaved <see cref="AvatarItem"/> rows for the whole template (for seeding).</summary>
    public static IReadOnlyList<AvatarItem> CreateItems() =>
        Template
            .Select(e => new AvatarItem
            {
                Slot = e.Slot,
                OptionValue = e.OptionValue,
                DisplayName = e.DisplayName,
                Cost = 0,
                Rarity = e.Rarity,
                IsDefault = e.IsDefault,
            })
            .ToList();
}
