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
        Task<ApiResult> LogActivityAsync(int workspaceId, int userId, string action, string entityType, int entityId, string? metadata = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả activity log của workspace.
        /// </summary>
        Task<ApiResult> GetWorkspaceActivityLogsAsync(int workspaceId, CancellationToken ct = default);

        /// <summary>
        /// Lấy activity log của user.
        /// </summary>
        Task<ApiResult> GetUserActivityLogsAsync(int userId, int workspaceId, CancellationToken ct = default);

        /// <summary>
        /// Lấy activity log của entity cụ thể.
        /// </summary>
        Task<ApiResult> GetEntityActivityLogsAsync(string entityType, int entityId, CancellationToken ct = default);

        /// <summary>
        /// Lấy activity log theo action.
        /// </summary>
        Task<ApiResult> GetActivityLogsByActionAsync(int workspaceId, string action, CancellationToken ct = default);

        /// <summary>
        /// Lấy activity log trong khoảng thời gian.
        /// </summary>
        Task<ApiResult> GetActivityLogsByDateRangeAsync(GetActivityLogDateRangeModel model, CancellationToken ct = default);

        /// <summary>
        /// Lấy activity log gần đây nhất.
        /// </summary>
        Task<ApiResult> GetRecentActivityLogsAsync(int workspaceId, int limit = 10, CancellationToken ct = default);

        /// <summary>
        /// Xóa activity log (cleanup cũ).
        /// </summary>
        Task<ApiResult> DeleteActivityLogAsync(int logId, CancellationToken ct = default);

        /// <summary>
        /// Xóa activity log cũ hơn ngày chỉ định.
        /// </summary>
        Task<ApiResult> DeleteOldActivityLogsAsync(int workspaceId, int daysOld, CancellationToken ct = default);

        /// <summary>
        /// Lấy thống kê hoạt động (số lần mỗi action).
        /// </summary>
        Task<ApiResult> GetActivityStatsAsync(int workspaceId, CancellationToken ct = default);
    }
}
