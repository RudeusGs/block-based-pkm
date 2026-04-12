using server.Service.Models;
using server.Service.Models.UserTaskPreference;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IUserTaskPreferenceService: Quản lý tùy chỉnh gợi ý task của user.
    /// </summary>
    public interface IUserTaskPreferenceService
    {
        Task<ApiResult> CreatePreferenceAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetPreferenceAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> UpdatePreferenceAsync(int userId, int workspaceId, UpdateUserTaskPreferenceModel model, CancellationToken ct = default);

        Task<ApiResult> UpdateWorkHoursAsync(UpdateWorkHoursModel model, CancellationToken ct = default);

        Task<ApiResult> UpdatePreferredDaysAsync(UpdatePreferredDaysModel model, CancellationToken ct = default);

        Task<ApiResult> UpdateSensitivityAsync(int userId, int workspaceId, int sensitivity, CancellationToken ct = default);

        Task<ApiResult> UpdateMinPriorityAsync(int userId, int workspaceId, string minPriority, CancellationToken ct = default);

        Task<ApiResult> UpdateRecommendationIntervalAsync(int userId, int workspaceId, int intervalMinutes, CancellationToken ct = default);

        Task<ApiResult> ToggleAutoRecommendationAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> UpdateMaxRecommendationsAsync(int userId, int workspaceId, int maxCount, CancellationToken ct = default);

        Task<ApiResult> ResetToDefaultAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> IsAutoRecommendationEnabledAsync(int userId, int workspaceId, CancellationToken ct = default);
    }
}
