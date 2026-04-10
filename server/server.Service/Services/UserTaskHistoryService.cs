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

        public async Task<ApiResult> RecordCompletionAsync(RecordTaskCompletionModel model, int userId)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsCompleted(model.Notes);
                await SaveChangesAsync();

                await _taskPerformanceMetricService.UpdateMetricOnCompletionAsync(new UpdateMetricOnCompletionModel
                {
                    TaskId = model.TaskId,
                    UserId = userId,
                    DurationMinutes = history.DurationMinutes
                });

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecordAbandonmentAsync(RecordAbandonmentModel model, int userId)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsAbandoned(model.Reason);
                await SaveChangesAsync();

                await _taskPerformanceMetricService.UpdateMetricOnAbandonmentAsync(model.TaskId, userId);

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecordSkipAsync(RecordSkipModel model, int userId)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories
                    .FirstOrDefaultAsync(h => h.TaskId == model.TaskId && h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress);

                if (history == null)
                {
                    history = new UserTaskHistory(model.TaskId, userId);
                    _dataContext.UserTaskHistories.Add(history);
                }

                history.MarkAsSkipped();
                await SaveChangesAsync();

                return ApiResult.Success(history);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserTaskHistoryAsync(int userId)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync();

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserTaskHistoryByWorkspaceAsync(int userId, int workspaceId)
        {
            try
            {
                var query = from h in _dataContext.UserTaskHistories
                            join t in _dataContext.Tasks on h.TaskId equals t.Id
                            where h.UserId == userId && t.WorkspaceId == workspaceId
                            orderby h.StartedAt descending
                            select h;

                var histories = await query.ToListAsync();
                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetTaskHistoryAsync(int taskId)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync();

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetHistoryByDateRangeAsync(GetHistoryDateRangeModel model)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == model.UserId && h.StartedAt >= model.StartDate && h.StartedAt <= model.EndDate)
                    .OrderByDescending(h => h.StartedAt)
                    .ToListAsync();

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetAverageDurationAsync(int taskId)
        {
            try
            {
                var avg = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.Status == StatusUserTaskHistory.Completed)
                    .AverageAsync(h => (double?)h.DurationMinutes) ?? 0;

                return ApiResult.Success(avg);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetUserAverageDurationAsync(int userId)
        {
            try
            {
                var avg = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId && h.Status == StatusUserTaskHistory.Completed)
                    .AverageAsync(h => (double?)h.DurationMinutes) ?? 0;

                return ApiResult.Success(avg);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionCountAsync(int taskId)
        {
            try
            {
                var count = await _dataContext.UserTaskHistories
                    .CountAsync(h => h.TaskId == taskId && h.Status == StatusUserTaskHistory.Completed);

                return ApiResult.Success(count);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionRateAsync(int taskId)
        {
            try
            {
                var total = await _dataContext.UserTaskHistories
                    .CountAsync(h => h.TaskId == taskId && (h.Status == StatusUserTaskHistory.Completed || h.Status == StatusUserTaskHistory.Abandoned));

                if (total == 0) return ApiResult.Success(0);

                var completed = await _dataContext.UserTaskHistories
                    .CountAsync(h => h.TaskId == taskId && h.Status == StatusUserTaskHistory.Completed);

                return ApiResult.Success((double)completed / total);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> DeleteHistoryAsync(int historyId)
        {
            try
            {
                var history = await _dataContext.UserTaskHistories.FindAsync(historyId);
                if (history == null) return ApiResult.Fail("History not found");

                _dataContext.UserTaskHistories.Remove(history);
                await SaveChangesAsync();

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecentCompletionsAsync(int userId, int limit = 10)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.UserId == userId && h.Status == StatusUserTaskHistory.Completed)
                    .OrderByDescending(h => h.CompletedAt)
                    .Take(limit)
                    .ToListAsync();

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
