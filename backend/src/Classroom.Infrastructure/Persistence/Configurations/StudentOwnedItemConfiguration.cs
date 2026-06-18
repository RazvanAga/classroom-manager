using Classroom.Domain.Avatars;
using Classroom.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class StudentOwnedItemConfiguration : IEntityTypeConfiguration<StudentOwnedItem>
{
    public void Configure(EntityTypeBuilder<StudentOwnedItem> builder)
    {
        // Composite PK (StudentId, ItemId) — this IS the UNIQUE(StudentId, ItemId) backstop the
        // transactional purchase relies on against double-buy (design.md §4.1).
        builder.HasKey(o => new { o.StudentId, o.ItemId });

        // Owning student; cascade so removing a student clears their inventory. No nav on Student.
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(o => o.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Catalog item. Restrict: a catalog option that students own must not be deletable out from
        // under them (the catalog is seeded, not edited, in v1).
        builder.HasOne<AvatarItem>()
            .WithMany()
            .HasForeignKey(o => o.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
