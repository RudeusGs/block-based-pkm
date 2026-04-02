using Microsoft.AspNetCore.SignalR;

namespace server.Infrastructure.Realtime
{
    public abstract class HubBase<THub> : Hub where THub : Hub
    {
        protected int CurrentUserId => int.TryParse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        protected string TraceId => Context?.ConnectionId ?? string.Empty;
    }
}
