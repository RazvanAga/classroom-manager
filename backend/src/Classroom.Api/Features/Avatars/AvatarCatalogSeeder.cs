using Classroom.Domain.Avatars;
using Classroom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Api.Features.Avatars;

public static class AvatarCatalogSeeder
{
    /// <summary>
    /// Seeds the <b>global</b> avatar catalog from <see cref="AvatarCatalog"/> (design.md §3.3).
    /// Unlike the per-class behavior catalog, this runs <b>once at startup</b>, not on class creation.
    /// Idempotent: only options not already present (by slot + value) are inserted, so it is safe to
    /// run on every boot and from the integration-test harness.
    /// </summary>
    public static async Task SeedAsync(ClassroomDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = await db.AvatarItems
            .Select(i => new { i.Slot, i.OptionValue })
            .ToListAsync(cancellationToken);
        var existingKeys = existing
            .Select(x => (x.Slot, x.OptionValue))
            .ToHashSet();

        var missing = AvatarCatalog.CreateItems()
            .Where(item => !existingKeys.Contains((item.Slot, item.OptionValue)))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.AvatarItems.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
