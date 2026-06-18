using Classroom.Domain.Classes;
using Classroom.Domain.Groups;
using Classroom.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class GroupingConfiguration : IEntityTypeConfiguration<Grouping>
{
    public void Configure(EntityTypeBuilder<Grouping> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(100);

        // A grouping belongs to exactly one class; deleting the class takes its groupings with it.
        builder.HasOne<Class>()
            .WithMany()
            .HasForeignKey(g => g.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Members are owned by the grouping: cascade so deleting a grouping clears its placements.
        builder.HasMany(g => g.Members)
            .WithOne()
            .HasForeignKey(m => m.GroupingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Read path: list a class's saved groupings most-recent-first.
        builder.HasIndex(g => g.ClassId);
    }
}

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.HasKey(m => m.Id);

        // The placed student. Students are only ever soft-deleted, so a saved placement keeps its
        // reference; the read join (against the Student global filter) just drops removed students.
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // A student appears at most once in any one grouping.
        builder.HasIndex(m => new { m.GroupingId, m.StudentId }).IsUnique();
    }
}
