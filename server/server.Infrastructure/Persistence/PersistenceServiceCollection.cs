using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace server.Infrastructure.Persistence;

public static class PersistenceServiceCollection
{
    /// <summary>
    /// Registers EF Core <see cref="DataContext"/> only. No Redis, SignalR, or other optional infrastructure.
    /// </summary>
    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Connection"));
        });

        return services;
    }
}
