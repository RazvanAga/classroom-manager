using Classroom.Domain.Avatars;
using Classroom.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class StudentEquippedConfiguration : IEntityTypeConfiguration<StudentEquipped>
{
    public void Configure(EntityTypeBuilder<StudentEquipped> builder)
    {
        // Composite PK (StudentId, Slot): at most one equipped option per slot; equipping upserts.
        builder.HasKey(e => new { e.StudentId, e.Slot });

        // Enums as strings (CLAUDE.md invariant).
        builder.Property(e => e.Slot)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Owning student; cascade so removing a student clears their equipped config. No nav on Student.
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Equipped catalog item. Restrict for the same reason as ownership: the seeded catalog isn't
        // edited in v1, and an equipped option must stay valid.
        builder.HasOne<AvatarItem>()
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
