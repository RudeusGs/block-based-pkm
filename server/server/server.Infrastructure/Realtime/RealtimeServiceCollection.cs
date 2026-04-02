using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace server.Infrastructure.Realtime
{
    public static class RealtimeServiceCollection
    {
        public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
            return services;
        }
    }
}
