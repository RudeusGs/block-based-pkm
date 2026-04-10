using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskPerformanceMetric;

namespace server.Service.Services
{
    public class TaskPerformanceMetricService : BaseService, ITaskPerformanceMetricService
    {
        public TaskPerformanceMetricService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> CreateMetricAsync(int taskId, int userId, int workspaceId)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == taskId && m.UserId == userId);

                if (metric == null)
                {
                    metric = new TaskPerformanceMetric(taskId, userId, workspaceId);
                    _dataContext.TaskPerformanceMetrics.Add(metric);
                    await SaveChangesAsync();
                }

                return ApiResult.Success(metric);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetMetricAsync(int taskId, int userId)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == taskId && m.UserId == userId);

                if (metric == null)
                    return ApiResult.Fail("Metric not found.");

                return ApiResult.Success(metric);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateMetricOnCompletionAsync(UpdateMetricOnCompletionModel model)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == model.TaskId && m.UserId == model.UserId);

                if (metric != null)
                {
                    metric.RecordCompletion(DateTime.UtcNow);
                    await SaveChangesAsync();
                    return ApiResult.Success(metric);
                }

                return ApiResult.Fail("Metric not found.");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateMetricOnAbandonmentAsync(int taskId, int userId)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == taskId && m.UserId == userId);

                if (metric != null)
                {
                    metric.RecordAbandonment();
                    await SaveChangesAsync();
                    return ApiResult.Success(metric);
                }

                return ApiResult.Fail("Metric not found.");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionRateAsync(int taskId, int userId)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == taskId && m.UserId == userId);

                if (metric == null)
                    return ApiResult.Success(0);

                int total = metric.CompletionCount + metric.AbandonedCount;
                double rate = total == 0 ? 0 : (double)metric.CompletionCount / total;

                return ApiResult.Success(rate);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetAverageDurationAsync(int taskId, int userId)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.UserId == userId && h.Status == StatusUserTaskHistory.Completed)
                    .ToListAsync();

                if (!histories.Any())
                    return ApiResult.Success(0);

                var avg = histories.Average(h => h.DurationMinutes);
                return ApiResult.Success(avg);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetOptimalCompletionHourAsync(int taskId, int userId)
        {
            try
            {
                var completedHistories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.UserId == userId && h.Status == StatusUserTaskHistory.Completed && h.CompletedAt.HasValue)
                    .ToListAsync();

                if (!completedHistories.Any())
                    return ApiResult.Success(null);

                var optimalHour = completedHistories
                    .GroupBy(h => h.CompletedAt!.Value.Hour)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                return ApiResult.Success(optimalHour);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetOptimalDayOfWeekAsync(int taskId, int userId)
        {
            try
            {
                var completedHistories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.UserId == userId && h.Status == StatusUserTaskHistory.Completed && h.CompletedAt.HasValue)
                    .ToListAsync();

                if (!completedHistories.Any())
                    return ApiResult.Success(null);

                var optimalDay = completedHistories
                    .GroupBy(h => h.CompletedAt!.Value.DayOfWeek)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                return ApiResult.Success(optimalDay);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetCompletionTrendAsync(int taskId, int userId)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => h.TaskId == taskId && h.UserId == userId && h.CompletedAt.HasValue)
                    .OrderBy(h => h.CompletedAt)
                    .Take(10)
                    .Select(h => new
                    {
                        CompletedAt = h.CompletedAt,
                        DurationMinutes = h.DurationMinutes
                    })
                    .ToListAsync();

                return ApiResult.Success(histories);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetDaysSinceLastCompletionAsync(int taskId, int userId)
        {
            try
            {
                var metric = await _dataContext.TaskPerformanceMetrics
                    .FirstOrDefaultAsync(m => m.TaskId == taskId && m.UserId == userId);

                if (metric == null || !metric.LastCompletedAt.HasValue)
                    return ApiResult.Success(null);

                var days = (DateTime.UtcNow - metric.LastCompletedAt.Value).TotalDays;
                return ApiResult.Success(days);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecalculateAllMetricsAsync(int workspaceId)
        {
            try
            {
                var histories = await _dataContext.UserTaskHistories
                    .Where(h => _dataContext.Tasks.Any(t => t.Id == h.TaskId && t.WorkspaceId == workspaceId))
                    .ToListAsync();

                var grouped = histories.GroupBy(h => new { h.TaskId, h.UserId });
                
                foreach (var group in grouped)
                {
                    var metric = await _dataContext.TaskPerformanceMetrics
                        .FirstOrDefaultAsync(m => m.TaskId == group.Key.TaskId && m.UserId == group.Key.UserId);

                    if (metric == null)
                    {
                        metric = new TaskPerformanceMetric(group.Key.TaskId, group.Key.UserId, workspaceId);
                        _dataContext.TaskPerformanceMetrics.Add(metric);
                    }
                }

                await SaveChangesAsync();
                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetTopPerformingTasksAsync(int userId, int workspaceId, int limit = 10)
        {
            try
            {
                var metrics = await _dataContext.TaskPerformanceMetrics
                    .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId)
                    .ToListAsync();

                var result = metrics
                    .Select(m => new
                    {
                        m.TaskId,
                        Total = m.CompletionCount + m.AbandonedCount,
                        Rate = (m.CompletionCount + m.AbandonedCount) == 0 ? 0 : (double)m.CompletionCount / (m.CompletionCount + m.AbandonedCount)
                    })
                    .Where(x => x.Total > 0)
                    .OrderByDescending(x => x.Rate)
                    .ThenByDescending(x => x.Total)
                    .Take(limit)
                    .ToList();

                return ApiResult.Success(result);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetLowPerformingTasksAsync(int userId, int workspaceId, int limit = 10)
        {
            try
            {
                var metrics = await _dataContext.TaskPerformanceMetrics
                    .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId)
                    .ToListAsync();

                var result = metrics
                    .Select(m => new
                    {
                        m.TaskId,
                        Total = m.CompletionCount + m.AbandonedCount,
                        Rate = (m.CompletionCount + m.AbandonedCount) == 0 ? 0 : (double)m.CompletionCount / (m.CompletionCount + m.AbandonedCount)
                    })
                    .Where(x => x.Total > 0)
                    .OrderBy(x => x.Rate)
                    .ThenByDescending(x => x.Total)
                    .Take(limit)
                    .ToList();

                return ApiResult.Success(result);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
