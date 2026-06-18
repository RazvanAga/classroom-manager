using Classroom.Domain.Avatars;

namespace Classroom.UnitTests.Avatars;

public class AvatarCatalogTests
{
    [Fact]
    public void Template_is_non_empty_and_covers_every_slot()
    {
        Assert.NotEmpty(AvatarCatalog.Template);

        var coveredSlots = AvatarCatalog.Template.Select(e => e.Slot).Distinct().ToList();
        Assert.Equal(Enum.GetValues<AvatarSlot>().Length, coveredSlots.Count);
    }

    [Fact]
    public void Every_slot_has_exactly_one_default_so_a_new_student_gets_a_complete_avatar()
    {
        foreach (var slot in Enum.GetValues<AvatarSlot>())
        {
            var defaults = AvatarCatalog.Template.Where(e => e.Slot == slot && e.IsDefault).ToList();
            Assert.True(defaults.Count == 1,
                $"Slot {slot} must have exactly one default option, but had {defaults.Count}.");
        }
    }

    [Fact]
    public void Defaults_returns_one_entry_per_slot()
    {
        var defaults = AvatarCatalog.Defaults();

        Assert.Equal(Enum.GetValues<AvatarSlot>().Length, defaults.Count);
        Assert.All(defaults, e => Assert.True(e.IsDefault));
        // One per slot — no slot represented twice.
        Assert.Equal(defaults.Count, defaults.Select(e => e.Slot).Distinct().Count());
    }

    [Fact]
    public void Option_values_are_unique_within_each_slot()
    {
        foreach (var slot in Enum.GetValues<AvatarSlot>())
        {
            var values = AvatarCatalog.Template.Where(e => e.Slot == slot).Select(e => e.OptionValue).ToList();
            Assert.Equal(values.Count, values.Distinct().Count());
        }
    }

    [Fact]
    public void CreateItems_materializes_one_row_per_template_entry_with_distinct_ids()
    {
        var items = AvatarCatalog.CreateItems();

        Assert.Equal(AvatarCatalog.Template.Count, items.Count);
        Assert.All(items, i => Assert.NotEqual(Guid.Empty, i.Id));
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void Defaults_are_free_and_every_non_default_is_priced_above_zero()
    {
        var items = AvatarCatalog.CreateItems();

        // Defaults are granted to new students, so they must never cost anything (design.md §3.3).
        Assert.All(items.Where(i => i.IsDefault), i => Assert.Equal(0, i.Cost));
        // Every buyable (non-default) option must carry a real price now the store exists (slice #9).
        Assert.Contains(items, i => !i.IsDefault);
        Assert.All(items.Where(i => !i.IsDefault), i => Assert.True(i.Cost > 0,
            $"Non-default option '{i.DisplayName}' must cost > 0, but cost {i.Cost}."));
    }

    [Theory]
    [InlineData(AvatarRarity.Common, 10)]
    [InlineData(AvatarRarity.Rare, 25)]
    [InlineData(AvatarRarity.Epic, 50)]
    [InlineData(AvatarRarity.Legendary, 100)]
    public void CostFor_prices_a_non_default_option_by_its_rarity(AvatarRarity rarity, int expected)
    {
        var entry = new AvatarCatalog.Entry(
            AvatarSlot.Hair, "value", "Name", IsDefault: false, rarity);

        Assert.Equal(expected, AvatarCatalog.CostFor(entry));
    }

    [Fact]
    public void CostFor_is_free_for_a_default_regardless_of_rarity()
    {
        var entry = new AvatarCatalog.Entry(
            AvatarSlot.Hair, "value", "Name", IsDefault: true, AvatarRarity.Legendary);

        Assert.Equal(0, AvatarCatalog.CostFor(entry));
    }
}
