using Classroom.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class ClassTeacherConfiguration : IEntityTypeConfiguration<ClassTeacher>
{
    public void Configure(EntityTypeBuilder<ClassTeacher> builder)
    {
        builder.HasKey(ct => new { ct.ClassId, ct.TeacherId });

        // Enums as strings (CLAUDE.md invariant).
        builder.Property(ct => ct.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(ct => ct.Teacher)
            .WithMany()
            .HasForeignKey(ct => ct.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
