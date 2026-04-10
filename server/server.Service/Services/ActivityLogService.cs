using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.ActivityLog;

namespace server.Service.Services
{
    public class ActivityLogService : BaseService, IActivityLogService
    {
        public ActivityLogService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> LogActivityAsync(int workspaceId, int userId, string action, string entityType, int entityId, string? metadata = null)
        {
            if (!Enum.TryParse<ActionActivityLog>(action, true, out var actionEnum))
                return ApiResult.Fail($"Invalid action: {action}");

            if (!Enum.TryParse<EntityTypeActivityLog>(entityType, true, out var entityTypeEnum))
                return ApiResult.Fail($"Invalid entityType: {entityType}");

            string description = $"{action} {entityType} {entityId}";

            var log = new ActivityLog(workspaceId, userId, actionEnum, entityTypeEnum, entityId, description, metadata, null);
            _dataContext.ActivityLogs.Add(log);
            await SaveChangesAsync();

            return ApiResult.Success(log);
        }

        public async Task<ApiResult> GetWorkspaceActivityLogsAsync(int workspaceId)
        {
            var logs = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == workspaceId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> GetUserActivityLogsAsync(int userId, int workspaceId)
        {
            var logs = await _dataContext.ActivityLogs
                .Where(l => l.UserId == userId && l.WorkspaceId == workspaceId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> GetEntityActivityLogsAsync(string entityType, int entityId)
        {
            if (!Enum.TryParse<EntityTypeActivityLog>(entityType, true, out var typeEnum))
                return ApiResult.Fail("Invalid entity type.");

            var logs = await _dataContext.ActivityLogs
                .Where(l => l.EntityType == typeEnum && l.EntityId == entityId)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> GetActivityLogsByActionAsync(int workspaceId, string action)
        {
            if (!Enum.TryParse<ActionActivityLog>(action, true, out var actionEnum))
                return ApiResult.Fail("Invalid action.");

            var logs = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == workspaceId && l.Action == actionEnum)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> GetActivityLogsByDateRangeAsync(GetActivityLogDateRangeModel model)
        {
            var logs = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == model.WorkspaceId && l.OccurredAt >= model.StartDate && l.OccurredAt <= model.EndDate)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> GetRecentActivityLogsAsync(int workspaceId, int limit = 10)
        {
            var logs = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == workspaceId)
                .OrderByDescending(l => l.OccurredAt)
                .Take(limit)
                .ToListAsync();

            return ApiResult.Success(logs);
        }

        public async Task<ApiResult> DeleteActivityLogAsync(int logId)
        {
            var log = await _dataContext.ActivityLogs.FindAsync(logId);
            if (log == null) return ApiResult.Fail("Log not found.");

            _dataContext.ActivityLogs.Remove(log);
            await SaveChangesAsync();
            return ApiResult.Success(true);
        }

        public async Task<ApiResult> DeleteOldActivityLogsAsync(int workspaceId, int daysOld)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysOld);
            var oldLogs = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == workspaceId && l.OccurredAt < cutoff)
                .ToListAsync();

            _dataContext.ActivityLogs.RemoveRange(oldLogs);
            await SaveChangesAsync();

            return ApiResult.Success(new { DeletedCount = oldLogs.Count });
        }

        public async Task<ApiResult> GetActivityStatsAsync(int workspaceId)
        {
            var stats = await _dataContext.ActivityLogs
                .Where(l => l.WorkspaceId == workspaceId)
                .GroupBy(l => l.Action)
                .Select(g => new
                {
                    Action = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return ApiResult.Success(stats);
        }
    }
}
