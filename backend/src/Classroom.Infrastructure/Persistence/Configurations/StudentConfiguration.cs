using Classroom.Domain.Classes;
using Classroom.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        // Enums as strings (CLAUDE.md invariant); nullable — left unset when no marker is given.
        builder.Property(s => s.Gender)
            .HasConversion<string>()
            .HasMaxLength(10);

        // A student belongs to exactly one class (design.md §1.3). No navigation is added to Class
        // to keep that entity untouched; the FK lives entirely on this side.
        builder.HasOne<Class>()
            .WithMany()
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete global query filter (design.md §7.1), matching ClassConfiguration: deleted
        // students vanish from every query. (Both ends of the FK share this DeletedAt filter.)
        builder.HasQueryFilter(s => s.DeletedAt == null);
    }
}
