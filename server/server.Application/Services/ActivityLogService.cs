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

        public async Task<ApiResult> LogActivityAsync(int workspaceId, int userId, string action, string entityType, int entityId, string? metadata = null, CancellationToken ct = default)
        {
            try
            {
                if (!Enum.TryParse<ActionActivityLog>(action, true, out var actionEnum))
                    return ApiResult.Fail($"Invalid action: {action}");

                if (!Enum.TryParse<EntityTypeActivityLog>(entityType, true, out var entityTypeEnum))
                    return ApiResult.Fail($"Invalid entityType: {entityType}");

                string description = $"{action} {entityType} {entityId}";

                var log = new ActivityLog(workspaceId, userId, actionEnum, entityTypeEnum, entityId, description, metadata, null);
                _dataContext.ActivityLogs.Add(log);
                await SaveChangesAsync(ct);

                return ApiResult.Success(log);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetWorkspaceActivityLogsAsync(int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == workspaceId)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserActivityLogsAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.UserId == userId && l.WorkspaceId == workspaceId)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetEntityActivityLogsAsync(string entityType, int entityId, CancellationToken ct = default)
        {
            try
            {
                if (!Enum.TryParse<EntityTypeActivityLog>(entityType, true, out var typeEnum))
                    return ApiResult.Fail("Invalid entity type.");

                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.EntityType == typeEnum && l.EntityId == entityId)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetActivityLogsByActionAsync(int workspaceId, string action, CancellationToken ct = default)
        {
            try
            {
                if (!Enum.TryParse<ActionActivityLog>(action, true, out var actionEnum))
                    return ApiResult.Fail("Invalid action.");

                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == workspaceId && l.Action == actionEnum)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetActivityLogsByDateRangeAsync(GetActivityLogDateRangeModel model, CancellationToken ct = default)
        {
            try
            {
                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == model.WorkspaceId && l.OccurredAt >= model.StartDate && l.OccurredAt <= model.EndDate)
                    .OrderByDescending(l => l.OccurredAt)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecentActivityLogsAsync(int workspaceId, int limit = 10, CancellationToken ct = default)
        {
            try
            {
                var logs = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == workspaceId)
                    .OrderByDescending(l => l.OccurredAt)
                    .Take(limit)
                    .ToListAsync(ct);

                return ApiResult.Success(logs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> DeleteActivityLogAsync(int logId, CancellationToken ct = default)
        {
            try
            {
                var log = await _dataContext.ActivityLogs.FindAsync(new object[] { logId }, ct);
                if (log == null) return ApiResult.Fail("Log not found.");

                _dataContext.ActivityLogs.Remove(log);
                await SaveChangesAsync(ct);
                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> DeleteOldActivityLogsAsync(int workspaceId, int daysOld, CancellationToken ct = default)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-daysOld);
                var oldLogs = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == workspaceId && l.OccurredAt < cutoff)
                    .ToListAsync(ct);

                _dataContext.ActivityLogs.RemoveRange(oldLogs);
                await SaveChangesAsync(ct);

                return ApiResult.Success(new { DeletedCount = oldLogs.Count });
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetActivityStatsAsync(int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var stats = await _dataContext.ActivityLogs
                    .Where(l => l.WorkspaceId == workspaceId)
                    .GroupBy(l => l.Action)
                    .Select(g => new
                    {
                        Action = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync(ct);

                return ApiResult.Success(stats);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
