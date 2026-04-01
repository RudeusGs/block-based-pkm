using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Infrastructure.Persistence;

public static class PersistenceServiceCollection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Connection"));
            // options.EnableDetailedErrors(); (Debug only not shown in production)
        });

        return services;
    }
}