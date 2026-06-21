using Classroom.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Reward-currency icon (slice #19): enum stored as a string (CLAUDE.md invariant), defaulting
        // to "Star" so existing rows and new classes start with the default without explicit init.
        builder.Property(c => c.CurrencyIcon)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(CurrencyIcon.Star);

        builder.HasMany(c => c.Teachers)
            .WithOne(ct => ct.Class)
            .HasForeignKey(ct => ct.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete: the one global query filter in the system (design.md §7.1). Archived
        // classes are NOT filtered here — they stay queryable so year-end history is retained.
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
