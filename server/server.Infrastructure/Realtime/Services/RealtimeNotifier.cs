using Microsoft.AspNetCore.SignalR;
using server.Infrastructure.Realtime.Hubs;
using server.Infrastructure.Realtime.Interfaces;

namespace server.Infrastructure.Realtime.Services
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<WorkspaceHub> _hubContext;

        public RealtimeNotifier(IHubContext<WorkspaceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToWorkspaceAsync(int workspaceId, string eventName, object payload)
        {
            try
            {
                await _hubContext.Clients
                    .Group(WorkspaceHub.GetWorkspaceGroupName(workspaceId))
                    .SendAsync(eventName, payload);
            }
            catch(Exception ex)
            {
                return;
            }
        }
    }
}