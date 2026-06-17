using Classroom.Domain.Behaviors;
using Classroom.Domain.Points;
using Classroom.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Persistence.Configurations;

public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .IsRequired();

        // Enums as strings (CLAUDE.md invariant).
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Reason)
            .HasMaxLength(500);

        // Owning student. Required, but students are only ever soft-deleted (never hard), so their
        // ledger history is retained; no nav is added to Student to keep that entity untouched.
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(t => t.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional source behavior. SetNull so editing the catalog (a hard delete, slice #5) never
        // erases point history — the row survives with a null BehaviorId.
        builder.HasOne<Behavior>()
            .WithMany()
            .HasForeignKey(t => t.BehaviorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Read paths: a student's balance (by student) and undo-by-batch (slice #7).
        builder.HasIndex(t => t.StudentId);
        builder.HasIndex(t => t.BatchId);
    }
}
