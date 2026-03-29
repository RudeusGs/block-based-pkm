using server.Service.Models;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskRecommendationService: Quản lý gợi ý task cho user.
    /// Dùng cho thuật toán gợi ý task dựa trên trọng số.
    /// </summary>
    public interface ITaskRecommendationService
    {
        /// <summary>
        /// Tính toán và sinh gợi ý task cho user tại thời điểm hiện tại.
        /// Dựa trên RecommendationWeight, OptimalHourOfDay, PreferredDaysOfWeek.
        /// </summary>
        Task<ApiResult> GenerateRecommendationsAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy gợi ý task pending cho user.
        /// </summary>
        Task<ApiResult> GetPendingRecommendationsAsync(int userId, int workspaceId);

        /// <summary>
        /// User chấp nhận gợi ý task.
        /// </summary>
        Task<ApiResult> AcceptRecommendationAsync(int recommendationId);

        /// <summary>
        /// User từ chối gợi ý task.
        /// </summary>
        Task<ApiResult> RejectRecommendationAsync(int recommendationId);

        /// <summary>
        /// User hoàn thành task từ gợi ý.
        /// </summary>
        Task<ApiResult> CompleteRecommendationAsync(int recommendationId);

        /// <summary>
        /// Lấy lịch sử gợi ý cho user.
        /// </summary>
        Task<ApiResult> GetRecommendationHistoryAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy gợi ý cụ thể.
        /// </summary>
        Task<ApiResult> GetRecommendationByIdAsync(int recommendationId);

        /// <summary>
        /// Lấy hiệu quả gợi ý (acceptance rate).
        /// </summary>
        Task<ApiResult> GetRecommendationEffectivenessAsync(int userId, int workspaceId);

        /// <summary>
        /// Xóa gợi ý hết hạn.
        /// </summary>
        Task<ApiResult> CleanupExpiredRecommendationsAsync();

        /// <summary>
        /// Tính lại trọng số gợi ý cho tất cả task (cron job).
        /// </summary>
        Task<ApiResult> RecalculateWeightsAsync(int workspaceId);

        /// <summary>
        /// Lấy top task được recommend nhiều nhất.
        /// </summary>
        Task<ApiResult> GetTopRecommendedTasksAsync(int workspaceId, int limit = 10);

        /// <summary>
        /// Lấy task có hiệu suất gợi ý cao nhất.
        /// </summary>
        Task<ApiResult> GetHighestScoringTasksAsync(int userId, int limit = 5);
    }
}
