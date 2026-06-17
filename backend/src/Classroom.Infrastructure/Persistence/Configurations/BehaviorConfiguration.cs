using Classroom.Domain.Behaviors;
using Classroom.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class BehaviorConfiguration : IEntityTypeConfiguration<Behavior>
{
    public void Configure(EntityTypeBuilder<Behavior> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.DefaultPoints)
            .IsRequired();

        // Per-class catalog (design.md §2.2/§2.3). No navigation is added to Class; the FK lives here.
        // No soft-delete query filter — behaviors are hard-deleted (not in the PRD's soft-delete set).
        builder.HasOne<Class>()
            .WithMany()
            .HasForeignKey(b => b.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
