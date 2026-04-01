using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

public static class IdentityServiceCollection
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<int>>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}