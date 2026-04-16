using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace server.Infrastructure.Realtime
{
    public abstract class HubBase<THub> : Hub where THub : Hub
    {
        protected int CurrentUserId
        {
            get
            {
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return int.TryParse(userIdClaim, out var id) ? id : 0;
            }
        }

        protected string TraceId => Context?.ConnectionId ?? string.Empty;
    }
}