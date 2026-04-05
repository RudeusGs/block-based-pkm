using server.Service.Models;
using server.Service.Models.ActivityLog;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IActivityLogService: Quản lý nhật ký hoạt động trong workspace.
    /// </summary>
    public interface IActivityLogService
    {
        /// <summary>
        /// Ghi log hoạt động (Create, Update, Delete).
        /// </summary>
        Task<ApiResult> LogActivityAsync(int workspaceId, int userId, string action, string entityType, int entityId, string? metadata = null);

        /// <summary>
        /// Lấy tất cả activity log của workspace.
        /// </summary>
        Task<ApiResult> GetWorkspaceActivityLogsAsync(int workspaceId);

        /// <summary>
        /// Lấy activity log của user.
        /// </summary>
        Task<ApiResult> GetUserActivityLogsAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy activity log của entity cụ thể.
        /// </summary>
        Task<ApiResult> GetEntityActivityLogsAsync(string entityType, int entityId);

        /// <summary>
        /// Lấy activity log theo action.
        /// </summary>
        Task<ApiResult> GetActivityLogsByActionAsync(int workspaceId, string action);

        /// <summary>
        /// Lấy activity log trong khoảng thời gian.
        /// </summary>
        Task<ApiResult> GetActivityLogsByDateRangeAsync(GetActivityLogDateRangeModel model);

        /// <summary>
        /// Lấy activity log gần đây nhất.
        /// </summary>
        Task<ApiResult> GetRecentActivityLogsAsync(int workspaceId, int limit = 10);

        /// <summary>
        /// Xóa activity log (cleanup cũ).
        /// </summary>
        Task<ApiResult> DeleteActivityLogAsync(int logId);

        /// <summary>
        /// Xóa activity log cũ hơn ngày chỉ định.
        /// </summary>
        Task<ApiResult> DeleteOldActivityLogsAsync(int workspaceId, int daysOld);

        /// <summary>
        /// Lấy thống kê hoạt động (số lần mỗi action).
        /// </summary>
        Task<ApiResult> GetActivityStatsAsync(int workspaceId);
    }
}
