using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using server.Infrastructure.Realtime.Hubs;

namespace server.Infrastructure.Realtime
{
    public static class RealtimeEndpointExtensions
    {
        public static IEndpointRouteBuilder MapRealtimeHubs(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<WorkspaceHub>("/hubs/workspace");

            // Add future hubs here:
            // endpoints.MapHub<AnotherHub>("/hubs/another");

            return endpoints;
        }
    }
}
