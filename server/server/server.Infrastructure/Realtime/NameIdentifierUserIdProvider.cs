using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace server.Infrastructure.Realtime
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
    }
}
