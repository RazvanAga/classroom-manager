using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Classroom.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context at design time (migrations) without booting the API.
/// The connection string here is only ever used to scaffold/compare migrations, never at runtime.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClassroomDbContext>
{
    public ClassroomDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClassroomDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=classroom;Username=classroom;Password=classroom")
            .Options;

        return new ClassroomDbContext(options);
    }
}
