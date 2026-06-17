using Classroom.Domain.Identity;
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
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Future feature slices configure their entities here / in IEntityTypeConfiguration types.
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // Invariant (CLAUDE.md): enums are stored as strings, not ints. Established now so
        // every later slice inherits it. EnumToStringConverter is applied per-enum as they appear.
    }
}
