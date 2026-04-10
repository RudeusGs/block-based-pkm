using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
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

        public TaskRecommendationService(
            DataContext dataContext,
            IUserService userService,
            IUserTaskPreferenceService preferenceService,
            IRealtimeNotifier notifier)
            : base(dataContext, userService)
        {
            _preferenceService = preferenceService;
            _notifier = notifier;
        }

        public async Task<ApiResult> GenerateRecommendationsAsync(int userId, int workspaceId)
        {
            try
            {
                // 1. Kiểm tra cấu hình gợi ý của User
                var prefResult = await _preferenceService.GetPreferenceAsync(userId, workspaceId);
                if (!prefResult.IsSuccess || prefResult.Data is not UserTaskPreference pref)
                {
                    return ApiResult.Fail("Không thể lấy cấu hình gợi ý của người dùng.");
                }

                if (!pref.IsSuitableForRecommendation(DateTime.UtcNow))
                {
                    return ApiResult.Fail("Hiện tại không nằm trong thời gian nhận gợi ý theo cấu hình của bạn.");
                }

                // 1b. Check if user is currently busy "không có task đang làm"
                var isUserBusy = await _dataContext.UserTaskHistories
                    .AnyAsync(h => h.UserId == userId && h.Status == StatusUserTaskHistory.InProgress);

                if (isUserBusy)
                {
                    return ApiResult.Fail("User đang thực hiện một task khác, không tạo thêm gợi ý lúc này.");
                }

                // 2. Lấy số lượng gợi ý hiện tại chưa hết hạn
                var activeCount = await _dataContext.TaskRecommendations
                    .CountAsync(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status == StatusTaskRecommendation.Pending);

                if (activeCount >= pref.MaxRecommendationsPerSession)
                {
                    return ApiResult.Fail("Đã đạt giới hạn số lượng gợi ý trong session hiện tại.");
                }

                // 3. Lấy các task đang mở và có thể làm
                var openTasks = await _dataContext.Tasks
                    .Where(t => t.WorkspaceId == workspaceId && t.Status == StatusWorkTask.ToDo && !t.IsDeleted)
                    .ToListAsync();

                // Lọc task chưa được gán hoặc đã được gán cho user hiện tại (trực tiếp hoặc qua team) - ở đây đơn giản hoá là task bất kỳ hoặc task assign cho mình
                var assignees = await _dataContext.TaskAssignees
                    .Where(a => a.UserId == userId)
                    .Select(a => a.TaskId)
                    .ToListAsync();

                var eligibleTasks = openTasks
                    .Where(t => assignees.Contains(t.Id) || !_dataContext.TaskAssignees.Any(a => a.TaskId == t.Id))
                    .Where(t => t.Priority >= pref.MinPriorityForRecommendation)
                    .ToList();

                // Lấy metrics của user để tính trọng số (completion rate)
                var userMetrics = await _dataContext.TaskPerformanceMetrics
                    .Where(m => m.UserId == userId && m.WorkspaceId == workspaceId)
                    .ToListAsync();

                var recommendations = new List<TaskRecommendation>();
                int addedCount = 0;

                foreach (var task in eligibleTasks)
                {
                    if (addedCount >= pref.MaxRecommendationsPerSession - activeCount)
                        break;

                    // Kiểm tra xem task này đã có gợi ý pending chưa
                    var hasPending = await _dataContext.TaskRecommendations
                        .AnyAsync(r => r.TaskId == task.Id && r.UserId == userId && r.Status == StatusTaskRecommendation.Pending);

                    if (hasPending) continue;

                    // Simple AI Score Algorithm:
                    decimal score = 50; // Base score

                    // 1. Dựa trên Priority
                    score += task.Priority switch
                    {
                        PriorityWorkTask.High => 30,
                        PriorityWorkTask.Medium => 15,
                        PriorityWorkTask.Low => 5,
                        _ => 0
                    };

                    // 2. Dựa trên DueDate
                    if (task.DueDate.HasValue)
                    {
                        var daysToDue = (task.DueDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                        if (daysToDue < 0) score += 20; // Quá hạn
                        else if (daysToDue == 0) score += 30; // Tới hạn hôm nay
                        else if (daysToDue <= 3) score += 10; // Sắp tới hạn
                    }

                    // 3. Trọng số task hoàn thành (Performance Metric)
                    var metric = userMetrics.FirstOrDefault(m => m.TaskId == task.Id);
                    if (metric != null)
                    {
                        var totalAttempts = metric.CompletionCount + metric.AbandonedCount;
                        if (totalAttempts > 0)
                        {
                            var rate = (double)metric.CompletionCount / totalAttempts;
                            score += (decimal)(rate * 20); // Thưởng tối đa 20 điểm nếu hay hoàn thành task này
                        }

                        // Nếu task này lâu rồi chưa làm, boost thêm để nhắc nhở
                        if (metric.LastCompletedAt.HasValue)
                        {
                            var daysSinceLast = (DateTime.UtcNow - metric.LastCompletedAt.Value).TotalDays;
                            if (daysSinceLast > 7) score += 10;
                        }
                    }

                    // Giới hạn max score là 100
                    score = Math.Min(100, score);
                    
                    // Nếu vượt qua độ nhạy của hệ thống (Ví dụ: Sensitivity = 50; score phải > 50 mới recommend)
                    // Thực tế Sensitivity càng cao thì hệ thống càng kén chọn (cần task thực sự urgent).
                    if (score >= pref.RecommendationSensitivity)
                    {
                        string reason = score >= 80 ? "Ưu tiên cao/Sắp tới hạn" : "Phù hợp để làm ngay";

                        var rec = new TaskRecommendation(task.Id, userId, workspaceId, score, reason, 24); // Tồn tại trong 24h
                        _dataContext.TaskRecommendations.Add(rec);
                        recommendations.Add(rec);
                        addedCount++;
                    }
                }

                if (recommendations.Any())
                {
                    await SaveChangesAsync();
                    
                    // Gửi thông báo thời gian thực ngay khi có gợi ý mới
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

        public async Task<ApiResult> GetPendingRecommendationsAsync(int userId, int workspaceId)
        {
            try
            {
                await CleanupExpiredRecommendationsAsync();

                var recs = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status == StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.Score)
                    .ToListAsync();

                return ApiResult.Success(recs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> AcceptRecommendationAsync(int recommendationId)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(recommendationId);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.CheckExpiration(); // Dựa vào thời gian lưu
                if (rec.Status == StatusTaskRecommendation.Expired)
                {
                    await SaveChangesAsync();
                    return ApiResult.Fail("Gợi ý đã hết hạn");
                }

                rec.Accept();
                await SaveChangesAsync();
                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RejectRecommendationAsync(int recommendationId)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(recommendationId);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.Reject();
                await SaveChangesAsync();
                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> CompleteRecommendationAsync(int recommendationId)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(recommendationId);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                if (rec.Status != StatusTaskRecommendation.Accepted)
                    return ApiResult.Fail("Phải chấp nhận gợi ý trước khi hoàn thành");

                rec.MarkCompleted();
                await SaveChangesAsync();
                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationHistoryAsync(int userId, int workspaceId)
        {
            try
            {
                var recs = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status != StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.RecommendedAt)
                    .Take(50) // Giới hạn lấy 50 cái gần nhất
                    .ToListAsync();

                return ApiResult.Success(recs);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationByIdAsync(int recommendationId)
        {
            try
            {
                var rec = await _dataContext.TaskRecommendations.FindAsync(recommendationId);
                if (rec == null) return ApiResult.Fail("Không tìm thấy gợi ý");

                rec.CheckExpiration();
                if (rec.Status == StatusTaskRecommendation.Expired)
                {
                    await SaveChangesAsync(); // Store the expired status
                }

                return ApiResult.Success(rec);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetRecommendationEffectivenessAsync(int userId, int workspaceId)
        {
            try
            {
                var history = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.WorkspaceId == workspaceId && r.Status != StatusTaskRecommendation.Pending && r.Status != StatusTaskRecommendation.Expired)
                    .ToListAsync();

                int total = history.Count;
                if (total == 0) return ApiResult.Success(0);

                // Tỷ lệ chấp nhận bao gồm Accepted, Completed
                int accepted = history.Count(r => r.Status == StatusTaskRecommendation.Accepted || r.Status == StatusTaskRecommendation.Completed);

                return ApiResult.Success((double)accepted / total);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> CleanupExpiredRecommendationsAsync()
        {
            try
            {
                // Tìm tất cả pending đã quá giờ
                var expired = await _dataContext.TaskRecommendations
                    .Where(r => r.Status == StatusTaskRecommendation.Pending && r.ExpiresAt.HasValue && r.ExpiresAt.Value < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var rec in expired)
                {
                    rec.CheckExpiration();
                }

                if (expired.Any())
                {
                    await SaveChangesAsync();
                }

                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> RecalculateWeightsAsync(int workspaceId)
        {
            try
            {
                // Trong bối cảnh cron job: Có thể update Score cho các recommendation pending dựa trên độ ưu tiên thay đổi.
                var pedingRecs = await _dataContext.TaskRecommendations
                    .Where(r => r.WorkspaceId == workspaceId && r.Status == StatusTaskRecommendation.Pending)
                    .ToListAsync();

                // Simple update: recalculate score 
                // .. To be implemented if advanced AI changes weight over time ..
                // .. For now we just return success ..
                return ApiResult.Success(true);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetTopRecommendedTasksAsync(int workspaceId, int limit = 10)
        {
            try
            {
                // Tìm các Task xuất hiện nhiều nhất trong recommendation
                var topTasks = await _dataContext.TaskRecommendations
                    .Where(r => r.WorkspaceId == workspaceId)
                    .GroupBy(r => r.TaskId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { TaskId = g.Key, Count = g.Count() })
                    .Take(limit)
                    .ToListAsync();

                return ApiResult.Success(topTasks);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetHighestScoringTasksAsync(int userId, int limit = 5)
        {
            try
            {
                var topScores = await _dataContext.TaskRecommendations
                    .Where(r => r.UserId == userId && r.Status == StatusTaskRecommendation.Pending)
                    .OrderByDescending(r => r.Score)
                    .Take(limit)
                    .ToListAsync();

                return ApiResult.Success(topScores);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
