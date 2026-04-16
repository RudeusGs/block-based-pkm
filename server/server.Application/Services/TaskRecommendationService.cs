using Microsoft.EntityFrameworkCore;
using server.Domain.Caching;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Domain.Realtime;
using server.Infrastructure.Persistence;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Service.Services
{
    public class TaskRecommendationService : BaseService, ITaskRecommendationService
    {
        private readonly IUserTaskPreferenceService _preferenceService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IRedisCacheService _redisCacheService;

        public TaskRecommendationService(
            DataContext dataContext,
            IUserService userService,
            IUserTaskPreferenceService preferenceService,
            IRealtimeNotifier notifier,
            IRedisCacheService redisCacheService)
            : base(dataContext, userService)
        {
            _preferenceService = preferenceService;
            _notifier = notifier;
            _redisCacheService = redisCacheService;
        }

        public async Task<ApiResult> GenerateRecommendationsAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                string cacheKey = $"recommendation:user:{userId}:workspace:{workspaceId}";
                var cachedRecs = await _redisCacheService.GetAsync<List<TaskRecommendation>>(cacheKey);
                if (cachedRecs != null && cachedRecs.Any())
                {
                    return ApiResult.Success(cachedRecs);
                }

                var prefResult = await _preferenceService.GetPreferenceAsync(userId, workspaceId, ct);
                if (!prefResult.IsSuccess || prefResult.Data is not UserTaskPreference pref)
                {
                    return ApiResult.Fail("Không thể lấy cấu hình gợi ý của người dùng.");
                }

                if (!pref.IsSuitableForRecommendation(DateTime.UtcNow))
                {
                    return ApiResult.Fail("Hiện tại không nằm trong thời gian nhận gợi ý theo cấu hình của bạn.");
                }

                var isUserBusy = await _dataContext.UserTaskHistories
                    .AnyAsync(h => h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress, ct);

                if (isUserBusy)
                {
                    return ApiResult.Fail("User đang thực hiện một task khác, không tạo thêm gợi ý lúc này.");
                }

                var activeCount = await _dataContext.TaskRecommendations
                    .CountAsync(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status == StatusTaskRecommendation.Pending, ct);

                if (activeCount >= pref.MaxRecommendationsPerSession)
                {
                    return ApiResult.Fail("Đã đạt giới hạn số lượng gợi ý trong session hiện tại.");
                }

                var openTasks = await _dataContext.Tasks
                    .Where(t => t.WorkspaceId == workspaceId && t.Status == StatusWorkTask.ToDo && !t.IsDeleted)
                    .Where(t => t.Priority >= pref.MinPriorityForRecommendation)
                    .ToListAsync(ct);

                if (openTasks.Count == 0)
                {
                    return ApiResult.Success(new List<TaskRecommendation>());
                }

                var openTaskIds = openTasks.ConvertAll(t => t.Id);

                var taskIdsWithAnyAssignee = await _dataContext.TaskAssignees
                    .AsNoTracking()
                    .Where(a => openTaskIds.Contains(a.TaskId))
                    .Select(a => a.TaskId)
                    .Distinct()
                    .ToListAsync(ct);
                var withAnyAssignee = taskIdsWithAnyAssignee.ToHashSet();

                var userAssignedTaskIds = await _dataContext.TaskAssignees
                    .AsNoTracking()
                    .Where(a => a.UserId == userId && openTaskIds.Contains(a.TaskId))
                    .Select(a => a.TaskId)
                    .Distinct()
                    .ToListAsync(ct);
                var userAssigned = userAssignedTaskIds.ToHashSet();

                var eligibleTasks = openTasks
                    .Where(t => !withAnyAssignee.Contains(t.Id) || userAssigned.Contains(t.Id))
                    .ToList();

                if (eligibleTasks.Count == 0)
                {
                    return ApiResult.Success(new List<TaskRecommendation>());
                }

                var eligibleTaskIds = eligibleTasks.ConvertAll(t => t.Id);

                var pendingRecommendedTaskIds = await _dataContext.TaskRecommendations
                    .AsNoTracking()
                    .Where(r => r.UserId == userId
                        && r.Status == StatusTaskRecommendation.Pending
                        && eligibleTaskIds.Contains(r.TaskId))
                    .Select(r => r.TaskId)
                    .Distinct()
                    .ToListAsync(ct);
                var pendingForTask = pendingRecommendedTaskIds.ToHashSet();

                var userMetrics = await _dataContext.TaskPerformanceMetrics
                    .AsNoTracking()
                    .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId && eligibleTaskIds.Contains(m.TaskId))
                    .ToListAsync(ct);
                var metricsByTaskId = userMetrics
                    .GroupBy(m => m.TaskId)
                    .ToDictionary(g => g.Key, g => g.First());

                var recommendations = new List<TaskRecommendation>();
                int addedCount = 0;

                foreach (var task in eligibleTasks)
                {
                    if (addedCount >= pref.MaxRecommendationsPerSession - activeCount)
                        break;

                    if (pendingForTask.Contains(task.Id))
                        continue;

                    decimal score = 50;

                    score += task.Priority switch
                    {
                        PriorityWorkTask.High => 30,
                        PriorityWorkTask.Medium => 15,
                        PriorityWorkTask.Low => 5,
                        _ => 0
                    };

                    if (task.DueDate.HasValue)
                    {
                        var daysToDue = (task.DueDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                        if (daysToDue < 0) score += 20;
                        else if (daysToDue == 0) score += 30;
                        else if (daysToDue <= 3) score += 10;
                    }

                    metricsByTaskId.TryGetValue(task.Id, out var metric);
                    if (metric != null)
                    {
                        var totalAttempts = metric.CompletionCount + metric.AbandonedCount;
                        if (totalAttempts > 0)
                        {
                            var rate = (double)metric.CompletionCount / totalAttempts;
                            score += (decimal)(rate * 20);
                        }

                        if (metric.LastCompletedAt.HasValue)
                        {
                            var daysSinceLast = (DateTime.UtcNow - metric.LastCompletedAt.Value).TotalDays;
                            if (daysSinceLast > 7) score += 10;
                        }
                    }

                    score = Math.Min(100, score);

                    if (score >= pref.RecommendationSensitivity)
                    {
                        string reason = score >= 80 ? "Ưu tiên cao/Sắp tới hạn" : "Phù hợp để làm ngay";

                        var rec = new TaskRecommendation(task.Id, userId, workspaceId, score, reason, 24);
                        _dataContext.TaskRecommendations.Add(rec);
                        recommendations.Add(rec);
                        addedCount++;
                    }
                }

                if (recommendations.Any())
                {
                    await SaveChangesAsync(ct);

                    await _redisCacheService.SetAsync(cacheKey, recommendations, TimeSpan.FromHours(1));

                    await ServiceHelper.SafeNotifyAsync(
                        _notifier,
                        workspaceId,
                        "NewTaskRecommendations",
                        new { UserId = userId, Count = addedCount }
                    );
                }

                return ApiResult.Success(recommendations);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetPendingRecommendationsAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                await CleanupExpiredRecommendationsAsync(ct);

                var recs = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status == StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.Score)
                    .ToListAsync(ct);

                return ApiResult.Success(recs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> AcceptRecommendationAsync(int recommendationId, CancellationToken ct = default)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(new object[] { recommendationId }, ct);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.CheckExpiration();
                if (rec.Status == StatusTaskRecommendation.Expired)
                {
                    await SaveChangesAsync(ct);
                    return ApiResult.Fail("Gợi ý đã hết hạn");
                }

                rec.Accept();
                await SaveChangesAsync(ct);

                await _redisCacheService.RemoveAsync($"recommendation:user:{rec.UserId}:workspace:{rec.WorkspaceId}");

                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RejectRecommendationAsync(int recommendationId, CancellationToken ct = default)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(new object[] { recommendationId }, ct);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.Reject();
                await SaveChangesAsync(ct);

                await _redisCacheService.RemoveAsync($"recommendation:user:{rec.UserId}:workspace:{rec.WorkspaceId}");

                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> CompleteRecommendationAsync(int recommendationId, CancellationToken ct = default)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(new object[] { recommendationId }, ct);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                if (rec.Status != StatusTaskRecommendation.Accepted)
                    return ApiResult.Fail("Phải chấp nhận gợi ý trước khi hoàn thành");

                rec.MarkCompleted();
                await SaveChangesAsync(ct);

                await _redisCacheService.RemoveAsync($"recommendation:user:{rec.UserId}:workspace:{rec.WorkspaceId}");

                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationHistoryAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var recs = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status != StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.RecommendedAt)
                    .Take(50)
                    .ToListAsync(ct);

                return ApiResult.Success(recs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationByIdAsync(int recommendationId, CancellationToken ct = default)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(new object[] { recommendationId }, ct);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.CheckExpiration();
                if (rec.Status == StatusTaskRecommendation.Expired)
                {
                    await SaveChangesAsync(ct);
                }

                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationEffectivenessAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var history = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status != StatusTaskRecommendation.Pending && r.Status != StatusTaskRecommendation.Expired)
                    .ToListAsync(ct);

                int total = history.Count;
                if (total == 0) return ApiResult.Success(0);

                int accepted = history.Count(r => r.Status == StatusTaskRecommendation.Accepted || r.Status == StatusTaskRecommendation.Completed);

                return ApiResult.Success((double)accepted / total);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> CleanupExpiredRecommendationsAsync(CancellationToken ct = default)
        {
            try
            {
                var expired = await _dataContext.TaskRecommendations
                    .Where(r => r.Status == StatusTaskRecommendation.Pending && r.ExpiresAt.HasValue && r.ExpiresAt.Value < DateTime.UtcNow)
                    .ToListAsync(ct);

                foreach (var rec in expired)
                {
                    rec.CheckExpiration();
                }

                if (expired.Any())
                {
                    await SaveChangesAsync(ct);
                }

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public Task<ApiResult> RecalculateWeightsAsync(int workspaceId, CancellationToken ct = default)
        {
            _ = ct;
            try
            {
                return Task.FromResult(ApiResult.Success(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }));
            }
        }

        public async Task<ApiResult> GetTopRecommendedTasksAsync(int workspaceId, int limit = 10, CancellationToken ct = default)
        {
            try
            {
                var topTasks = await _dataContext.TaskRecommendations
                    .Where(r => r.WorkspaceId == workspaceId)
                    .GroupBy(r => r.TaskId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { TaskId = g.Key, Count = g.Count() })
                    .Take(limit)
                    .ToListAsync(ct);

                return ApiResult.Success(topTasks);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetHighestScoringTasksAsync(int userId, int limit = 5, CancellationToken ct = default)
        {
            try
            {
                var topScores = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.Status == StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.Score)
                    .Take(limit)
                    .ToListAsync(ct);

                return ApiResult.Success(topScores);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
