using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace server.Infrastructure.Realtime.Hubs
{
    [Authorize]
    public class WorkspaceHub : Hub
    {
        public async Task JoinWorkspace(int workspaceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetWorkspaceGroupName(workspaceId));
        }

        public async Task LeaveWorkspace(int workspaceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetWorkspaceGroupName(workspaceId));
        }

        public static string GetWorkspaceGroupName(int workspaceId) => $"workspace:{workspaceId}";
    }
}
