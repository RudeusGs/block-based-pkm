using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Service.Services
{
    public class RealtimeSessionService : BaseService, IRealtimeSessionService
    {
        private readonly IRealtimeNotifier _realtimeNotifier;

        public RealtimeSessionService(
            DataContext dataContext, 
            IUserService userService,
            IRealtimeNotifier realtimeNotifier) 
            : base(dataContext, userService)
        {
            _realtimeNotifier = realtimeNotifier;
        }

        public async Task<ApiResult> CreateSessionAsync(int userId, int workspaceId, string connectionId, int? pageId = null)
        {
            var session = new RealtimeSession(userId, workspaceId, connectionId, pageId);
            _dataContext.RealtimeSessions.Add(session);
            await SaveChangesAsync();

            await _realtimeNotifier.SendToWorkspaceAsync(workspaceId, "UserJoined", new 
            { 
                UserId = userId, 
                PageId = pageId, 
                SessionId = session.Id 
            });

            return ApiResult.Success(session, "Thêm session thành công.");
        }

        public async Task<ApiResult> UpdateSessionAsync(int sessionId)
        {
            var session = await _dataContext.RealtimeSessions.FindAsync(sessionId);
            if (session == null) 
                return ApiResult.Fail("Session không tồn tại.");

            session.Ping();
            await SaveChangesAsync();

            return ApiResult.Success(session);
        }

        public async Task<ApiResult> UpdateSessionPageAsync(int sessionId, int? pageId)
        {
            var session = await _dataContext.RealtimeSessions.FindAsync(sessionId);
            if (session == null) 
                return ApiResult.Fail("Session không tồn tại.");

            session.MoveToPage(pageId);
            await SaveChangesAsync();

            await _realtimeNotifier.SendToWorkspaceAsync(session.WorkspaceId, "UserMovedPage", new 
            { 
                UserId = session.UserId, 
                SessionId = session.Id, 
                PageId = pageId 
            });

            return ApiResult.Success(session);
        }

        public async Task<ApiResult> EndSessionAsync(int sessionId)
        {
            var session = await _dataContext.RealtimeSessions.FindAsync(sessionId);
            if (session == null) 
                return ApiResult.Fail("Session không tồn tại.");

            _dataContext.RealtimeSessions.Remove(session);
            await SaveChangesAsync();

            await _realtimeNotifier.SendToWorkspaceAsync(session.WorkspaceId, "UserLeft", new 
            { 
                UserId = session.UserId, 
                SessionId = session.Id 
            });

            return ApiResult.Success(true);
        }

        public async Task<ApiResult> GetUserSessionAsync(int userId, int workspaceId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);

            var session = await _dataContext.RealtimeSessions
                .Where(s => s.UserId == userId && s.WorkspaceId == workspaceId && s.LastPing >= activeTime)
                .OrderByDescending(s => s.LastPing)
                .FirstOrDefaultAsync();

            return session != null 
                ? ApiResult.Success(session) 
                : ApiResult.Fail("Không có session active.");
        }

        public async Task<ApiResult> GetOnlineUsersAsync(int workspaceId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);

            var users = await _dataContext.RealtimeSessions
                .Where(s => s.WorkspaceId == workspaceId && s.LastPing >= activeTime)
                .GroupBy(s => s.UserId)
                .Select(g => g.OrderByDescending(s => s.LastPing).First())
                .Select(s => new {
                    s.UserId,
                    s.PageId,
                    s.LastPing
                })
                .ToListAsync();

            return ApiResult.Success(users);
        }

        public async Task<ApiResult> GetUsersViewingPageAsync(int pageId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);

            var users = await _dataContext.RealtimeSessions
                .Where(s => s.PageId == pageId && s.LastPing >= activeTime)
                .GroupBy(s => s.UserId)
                .Select(g => g.OrderByDescending(s => s.LastPing).First())
                .Select(s => new {
                    s.UserId,
                    s.LastPing
                })
                .ToListAsync();

            return ApiResult.Success(users);
        }

        public async Task<ApiResult> IsUserOnlineAsync(int userId, int workspaceId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);

            var isOnline = await _dataContext.RealtimeSessions
                .AnyAsync(s => s.UserId == userId && s.WorkspaceId == workspaceId && s.LastPing >= activeTime);

            return ApiResult.Success(new { IsOnline = isOnline });
        }

        public async Task<ApiResult> CleanupExpiredSessionsAsync(int timeoutMinutes = 30)
        {
            var expiredTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            
            var expiredSessions = await _dataContext.RealtimeSessions
                .Where(s => s.LastPing < expiredTime)
                .ToListAsync();

            if (expiredSessions.Any())
            {
                _dataContext.RealtimeSessions.RemoveRange(expiredSessions);
                await SaveChangesAsync();
            }

            return ApiResult.Success(expiredSessions.Count);
        }

        public async Task<ApiResult> GetOnlineCountAsync(int workspaceId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);

            var count = await _dataContext.RealtimeSessions
                .Where(s => s.WorkspaceId == workspaceId && s.LastPing >= activeTime)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            return ApiResult.Success(count);
        }

        public async Task<ApiResult> GetRealtimeActivityAsync(int workspaceId)
        {
            var activeTime = DateTime.UtcNow.AddSeconds(-60);
            var activities = await _dataContext.RealtimeSessions
                .Where(s => s.WorkspaceId == workspaceId && s.LastPing >= activeTime)
                .OrderByDescending(s => s.LastPing)
                .Select(s => new {
                    s.Id,
                    s.UserId,
                    s.PageId,
                    s.ConnectedAt,
                    s.LastPing,
                    Status = "Active"
                })
                .ToListAsync();

            return ApiResult.Success(activities);
        }
    }
}
