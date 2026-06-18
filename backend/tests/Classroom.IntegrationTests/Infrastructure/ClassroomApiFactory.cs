using Classroom.Api.Features.Avatars;
using Classroom.Api.Identity;
using Classroom.Infrastructure;
using Classroom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Classroom.IntegrationTests.Infrastructure;

/// <summary>
/// The reusable integration-test harness (the reference pattern later slices copy):
/// a real PostgreSQL started via Testcontainers, the real app booted through
/// <see cref="WebApplicationFactory{TEntryPoint}"/>, real EF migrations applied, and the
/// teacher seeded. Shared once across all integration tests via the <c>Api</c> collection.
/// </summary>
public class ClassroomApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SeedEmail = "teacher@classroom.local";
    public const string SeedPassword = "Passw0rd!";
    public const string SeedDisplayName = "Demo Teacher";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" environment: the app skips its dev-only auto-migrate/seed; the harness
        // controls schema + seed below so test setup is explicit and deterministic.
        builder.UseEnvironment("Testing");

        // Pin the seed identity so tests don't depend on appsettings values (read post-build).
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedTeacher:Email"] = SeedEmail,
                ["SeedTeacher:Password"] = SeedPassword,
                ["SeedTeacher:DisplayName"] = SeedDisplayName,
            });
        });

        // Repoint EF at the Testcontainers database. Done here (ConfigureTestServices runs after
        // the app's own registrations) because Program reads the connection string before Build(),
        // so a configuration override alone would be too late.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ClassroomDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddInfrastructure(_database.GetConnectionString());
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // First access to Services builds the host (reading the container connection string).
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
        await context.Database.MigrateAsync();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await SeedData.SeedTeacherAsync(scope.ServiceProvider, configuration);

        // The avatar catalog is global and seeded once at startup (design.md §3.3); the dev path does
        // this in Program, but the "Testing" environment skips that, so the harness seeds it here.
        await AvatarCatalogSeeder.SeedAsync(context);
    }

    // Explicit: xunit's IAsyncLifetime.DisposeAsync returns Task, the base returns ValueTask.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ClassroomApiFactory>
{
    public const string Name = "Api";
}
