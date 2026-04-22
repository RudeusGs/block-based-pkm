using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Time;

namespace Pkm.Infrastructure.Time;

public static class TimeServiceCollection
{
    public static IServiceCollection AddTimeInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}