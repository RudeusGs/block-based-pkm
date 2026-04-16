using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Realtime.Hubs
{
    [Authorize]
    public class WorkspaceHub : HubBase<WorkspaceHub>
    {
        private readonly DataContext _dataContext;

        public WorkspaceHub(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
        public async Task JoinWorkspace(int workspaceId)
        {
            if (workspaceId <= 0)
                throw new HubException("WorkspaceId không hợp lệ.");

            var userId = CurrentUserId;
            if (userId <= 0)
                throw new HubException("Unauthorized");

            var workspaceExists = await _dataContext.Set<Workspace>()
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted);

            if (workspaceExists == null)
                throw new HubException("Workspace không tồn tại.");

            var isMember = await _dataContext.Set<WorkspaceMember>()
                .AsNoTracking()
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

            var isOwner = await _dataContext.Set<Workspace>()
                .AsNoTracking()
                .AnyAsync(w => w.Id == workspaceId && w.OwnerId == userId);

            if (!isMember && !isOwner)
                throw new HubException("Bạn không có quyền tham gia workspace này.");

            await AddToGroupAsync(Context.ConnectionId, GetWorkspaceGroupName(workspaceId));
        }

        protected virtual Task AddToGroupAsync(string connectionId, string groupName)
            => Groups.AddToGroupAsync(connectionId, groupName);

        public async Task LeaveWorkspace(int workspaceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetWorkspaceGroupName(workspaceId));
        }

        public static string GetWorkspaceGroupName(int workspaceId) => $"workspace:{workspaceId}";
    }
}
