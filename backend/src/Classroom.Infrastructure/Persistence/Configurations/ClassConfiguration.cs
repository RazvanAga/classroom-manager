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

        builder.HasMany(c => c.Teachers)
            .WithOne(ct => ct.Class)
            .HasForeignKey(ct => ct.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete: the one global query filter in the system (design.md §7.1). Archived
        // classes are NOT filtered here — they stay queryable so year-end history is retained.
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
