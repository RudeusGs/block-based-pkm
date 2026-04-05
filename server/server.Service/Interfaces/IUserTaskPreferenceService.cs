using server.Service.Models;
using server.Service.Models.UserTaskPreference;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IUserTaskPreferenceService: Quản lý tùy chỉnh gợi ý task của user.
    /// </summary>
    public interface IUserTaskPreferenceService
    {
        /// <summary>
        /// Tạo tùy chỉnh mới (default values).
        /// </summary>
        Task<ApiResult> CreatePreferenceAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy tùy chỉnh của user.
        /// </summary>
        Task<ApiResult> GetPreferenceAsync(int userId, int workspaceId);

        /// <summary>
        /// Cập nhật tùy chỉnh của user.
        /// </summary>
        Task<ApiResult> UpdatePreferenceAsync(int userId, int workspaceId, UpdateUserTaskPreferenceModel model);

        /// <summary>
        /// Cập nhật giờ làm việc.
        /// </summary>
        Task<ApiResult> UpdateWorkHoursAsync(UpdateWorkHoursModel model);

        /// <summary>
        /// Cập nhật ngày ưu tiên (Mon-Fri, ...).
        /// </summary>
        Task<ApiResult> UpdatePreferredDaysAsync(UpdatePreferredDaysModel model);

        /// <summary>
        /// Cập nhật độ nhạy của gợi ý (0-100).
        /// </summary>
        Task<ApiResult> UpdateSensitivityAsync(int userId, int workspaceId, int sensitivity);

        /// <summary>
        /// Cập nhật mức độ ưu tiên tối thiểu.
        /// </summary>
        Task<ApiResult> UpdateMinPriorityAsync(int userId, int workspaceId, string minPriority);

        /// <summary>
        /// Cập nhật khoảng cách giữa gợi ý.
        /// </summary>
        Task<ApiResult> UpdateRecommendationIntervalAsync(int userId, int workspaceId, int intervalMinutes);

        /// <summary>
        /// Bật/tắt tính năng auto recommendation.
        /// </summary>
        Task<ApiResult> ToggleAutoRecommendationAsync(int userId, int workspaceId);

        /// <summary>
        /// Cập nhật số lượng gợi ý tối đa.
        /// </summary>
        Task<ApiResult> UpdateMaxRecommendationsAsync(int userId, int workspaceId, int maxCount);

        /// <summary>
        /// Reset tùy chỉnh về mặc định.
        /// </summary>
        Task<ApiResult> ResetToDefaultAsync(int userId, int workspaceId);

        /// <summary>
        /// Kiểm tra user có bật auto recommendation không.
        /// </summary>
        Task<ApiResult> IsAutoRecommendationEnabledAsync(int userId, int workspaceId);
    }
}
