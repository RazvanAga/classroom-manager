using Classroom.Domain.Avatars;
using Classroom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Avatars;

public static class AvatarCatalogSeeder
{
    /// <summary>
    /// Seeds the <b>global</b> avatar catalog from <see cref="AvatarCatalog"/> (design.md §3.3).
    /// Unlike the per-class behavior catalog, this runs <b>once at startup</b>, not on class creation.
    /// Idempotent: options not already present (by slot + value) are inserted; existing rows have their
    /// <see cref="AvatarItem.Cost"/>/<see cref="AvatarItem.Rarity"/> reconciled to the in-code template
    /// (slice #9 priced the catalog, and the template is the single source of truth — so re-pricing is a
    /// code edit, not a data migration). Safe to run on every boot and from the integration-test harness.
    /// </summary>
    public static async Task SeedAsync(ClassroomDbContext db, CancellationToken cancellationToken = default)
    {
        var template = AvatarCatalog.CreateItems();
        var existing = await db.AvatarItems.ToListAsync(cancellationToken);
        var existingByKey = existing.ToDictionary(i => (i.Slot, i.OptionValue));

        var missing = new List<AvatarItem>();
        var changed = false;

        foreach (var item in template)
        {
            if (existingByKey.TryGetValue((item.Slot, item.OptionValue), out var current))
            {
                // Reconcile mutable catalog metadata for an already-seeded option (e.g. #9 pricing).
                if (current.Cost != item.Cost || current.Rarity != item.Rarity)
                {
                    current.Cost = item.Cost;
                    current.Rarity = item.Rarity;
                    changed = true;
                }
            }
            else
            {
                missing.Add(item);
            }
        }

        if (missing.Count == 0 && !changed)
        {
            return;
        }

        db.AvatarItems.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
