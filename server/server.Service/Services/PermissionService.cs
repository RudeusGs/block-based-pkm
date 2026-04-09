using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Interfaces;

namespace server.Service.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly DataContext _context;

        public PermissionService(DataContext context)
        {
            _context = context;
        }

        public async Task<bool> HasWorkspaceAccessAsync(int workspaceId, int userId, CancellationToken ct)
        {
            var isOwner = await _context.Set<Workspace>()
                .AnyAsync(w => w.Id == workspaceId && w.OwnerId == userId, ct);

            if (isOwner) return true;

            return await _context.Set<WorkspaceMember>()
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
        }
    }
}
