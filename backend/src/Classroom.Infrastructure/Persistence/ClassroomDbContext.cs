using Classroom.Domain.Avatars;
using Classroom.Domain.Behaviors;
using Classroom.Domain.Classes;
using Classroom.Domain.Identity;
using Classroom.Domain.Points;
using Classroom.Domain.Students;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. Backed by PostgreSQL. Identity tables use
/// <see cref="Guid"/> keys (GUID v7 is assigned in application code, see design.md §8.5).
/// </summary>
public class ClassroomDbContext(DbContextOptions<ClassroomDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassTeacher> ClassTeachers => Set<ClassTeacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Behavior> Behaviors => Set<Behavior>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<AvatarItem> AvatarItems => Set<AvatarItem>();
    public DbSet<StudentOwnedItem> StudentOwnedItems => Set<StudentOwnedItem>();
    public DbSet<StudentEquipped> StudentEquipped => Set<StudentEquipped>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Feature slices configure their entities via IEntityTypeConfiguration in this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(ClassroomDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Invariant (CLAUDE.md): enums are stored as strings, not ints. Established now so
        // every later slice inherits it. EnumToStringConverter is applied per-enum as they appear.
    }
}
