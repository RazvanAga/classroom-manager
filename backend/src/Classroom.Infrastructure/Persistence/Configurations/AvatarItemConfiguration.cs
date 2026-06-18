using Classroom.Domain.Avatars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class AvatarItemConfiguration : IEntityTypeConfiguration<AvatarItem>
{
    public void Configure(EntityTypeBuilder<AvatarItem> builder)
    {
        builder.HasKey(i => i.Id);

        // Enums as strings (CLAUDE.md invariant). Rarity is nullable (plain options have none).
        builder.Property(i => i.Slot)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Rarity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.OptionValue)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Cost)
            .IsRequired();

        // Global catalog (design.md §3.3): no ClassId. One row per (slot, option value); the unique
        // index also makes the startup seed idempotent (re-seeding can't duplicate an option).
        builder.HasIndex(i => new { i.Slot, i.OptionValue }).IsUnique();
    }
}
