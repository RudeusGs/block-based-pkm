using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskPerformanceMetric;
using server.Service.Models.UserTaskHistory;

namespace server.Service.Services
{
    public class UserTaskHistoryService : BaseService, IUserTaskHistoryService
    {
        private readonly ITaskPerformanceMetricService _taskPerformanceMetricService;

        public UserTaskHistoryService(
            DataContext dataContext,
            IUserService userService,
            ITaskPerformanceMetricService taskPerformanceMetricService)
            : base(dataContext, userService)
        {
            _taskPerformanceMetricService = taskPerformanceMetricService;
        }

        public async Task<ApiResult> RecordCompletionAsync(RecordTaskCompletionModel model, int userId, CancellationToken ct = default)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress, ct);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsCompleted(model.Notes);
                await SaveChangesAsync(ct);

                await _taskPerformanceMetricService.UpdateMetricOnCompletionAsync(new UpdateMetricOnCompletionModel
                {
                    TaskId = model.TaskId,
                    UserId = userId,
                    DurationMinutes = history.DurationMinutes
                }, ct);

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecordAbandonmentAsync(RecordAbandonmentModel model, int userId, CancellationToken ct = default)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress, ct);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsAbandoned(model.Reason);
                await SaveChangesAsync(ct);

                await _taskPerformanceMetricService.UpdateMetricOnAbandonmentAsync(model.TaskId, userId, ct);

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecordSkipAsync(RecordSkipModel model, int userId, CancellationToken ct = default)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress, ct);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsSkipped();
                await SaveChangesAsync(ct);

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserTaskHistoryAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync(ct);

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserTaskHistoryByWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var query = from h in _dataContext.UserTaskHistories
                            join t in _dataContext.Tasks on h.TaskId equals t.Id
                            where h.UserId == userId && t.WorkspaceId == workspaceId
                            orderby h.StartedAt descending
                            select h;

                var histories = await query.ToListAsync(ct);
                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetTaskHistoryAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync(ct);

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetHistoryByDateRangeAsync(GetHistoryDateRangeModel model, CancellationToken ct = default)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == model.UserId && h.StartedAt >= model.StartDate && h.StartedAt <= model.EndDate)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync(ct);

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetAverageDurationAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var avg = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.Status == StatusUserTaskHistory.Completed)
                    .AverageAsync(h => (double?)h.DurationMinutes, ct) ?? 0;

                return ApiResult.Success(avg);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserAverageDurationAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var avg = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId && h.Status == StatusUserTaskHistory.Completed)
                    .AverageAsync(h => (double?)h.DurationMinutes, ct) ?? 0;

                return ApiResult.Success(avg);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionCountAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var count = await _dataContext.UserTaskHistories
                    .CountAsync(h => h.TaskId == taskId && h.Status == StatusUserTaskHistory.Completed, ct);

                return ApiResult.Success(count);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionRateAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var summary = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId)
                    .Where(h => h.Status == StatusUserTaskHistory.Completed || h.Status == StatusUserTaskHistory.Abandoned)
                    .GroupBy(h => 1)
                    .Select(g => new
                    {
                        Total = g.Count(),
                        Completed = g.Count(h => h.Status == StatusUserTaskHistory.Completed)
                    })
                    .FirstOrDefaultAsync(ct);

                if (summary == null || summary.Total == 0)
                    return ApiResult.Success(0);

                return ApiResult.Success((double)summary.Completed / summary.Total);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> DeleteHistoryAsync(int historyId, CancellationToken ct = default)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories.FindAsync(new object[] { historyId }, ct);
                if (history == null) return ApiResult.Fail("Không tìm thấy lịch sử", "NOT_FOUND");

                _dataContext.UserTaskHistories.Remove(history);
                await SaveChangesAsync(ct);

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecentCompletionsAsync(int userId, int limit = 10, CancellationToken ct = default)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId && h.Status == StatusUserTaskHistory.Completed)
                    .OrderByDescending(h => h.CompletedAt)
                    .Take(limit)
                    .ToListAsync(ct);

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
