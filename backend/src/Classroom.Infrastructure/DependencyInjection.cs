using Classroom.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core context against PostgreSQL. The connection string is supplied by
    /// the host so Infrastructure stays free of configuration-source concerns.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ClassroomDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
