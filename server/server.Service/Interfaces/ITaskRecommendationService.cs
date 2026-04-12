using server.Service.Models;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskRecommendationService: Quản lý gợi ý task cho user.
    /// Dùng cho thuật toán gợi ý task dựa trên trọng số.
    /// </summary>
    public interface ITaskRecommendationService
    {
        Task<ApiResult> GenerateRecommendationsAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetPendingRecommendationsAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> AcceptRecommendationAsync(int recommendationId, CancellationToken ct = default);

        Task<ApiResult> RejectRecommendationAsync(int recommendationId, CancellationToken ct = default);

        Task<ApiResult> CompleteRecommendationAsync(int recommendationId, CancellationToken ct = default);

        Task<ApiResult> GetRecommendationHistoryAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetRecommendationByIdAsync(int recommendationId, CancellationToken ct = default);

        Task<ApiResult> GetRecommendationEffectivenessAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> CleanupExpiredRecommendationsAsync(CancellationToken ct = default);

        Task<ApiResult> RecalculateWeightsAsync(int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetTopRecommendedTasksAsync(int workspaceId, int limit = 10, CancellationToken ct = default);

        Task<ApiResult> GetHighestScoringTasksAsync(int userId, int limit = 5, CancellationToken ct = default);
    }
}
