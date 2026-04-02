using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using server.Infrastructure.Realtime.Interfaces;
using server.Infrastructure.Realtime.Services;

namespace server.Infrastructure.Realtime
{
    public static class RealtimeServiceCollection
    {
        public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
            services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
            return services;
        }
    }
}
