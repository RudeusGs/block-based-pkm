using server.Service.Models;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IRealtimeSessionService: Quản lý phiên làm việc thời gian thực.
    /// </summary>
    public interface IRealtimeSessionService
    {
        /// <summary>
        /// Tạo session khi user connect.
        /// </summary>
        Task<ApiResult> CreateSessionAsync(int userId, int workspaceId, int? pageId = null);

        /// <summary>
        /// Cập nhật session (LastPing).
        /// </summary>
        Task<ApiResult> UpdateSessionAsync(int sessionId);

        /// <summary>
        /// Cập nhật page hiện tại của session.
        /// </summary>
        Task<ApiResult> UpdateSessionPageAsync(int sessionId, int? pageId);

        /// <summary>
        /// Kết thúc session khi user disconnect.
        /// </summary>
        Task<ApiResult> EndSessionAsync(int sessionId);

        /// <summary>
        /// Lấy session của user.
        /// </summary>
        Task<ApiResult> GetUserSessionAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy tất cả user đang online trong workspace.
        /// </summary>
        Task<ApiResult> GetOnlineUsersAsync(int workspaceId);

        /// <summary>
        /// Lấy tất cả user đang xem page.
        /// </summary>
        Task<ApiResult> GetUsersViewingPageAsync(int pageId);

        /// <summary>
        /// Kiểm tra user có online không.
        /// </summary>
        Task<ApiResult> IsUserOnlineAsync(int userId, int workspaceId);

        /// <summary>
        /// Cleanup session quá lâu không ping.
        /// </summary>
        Task<ApiResult> CleanupExpiredSessionsAsync(int timeoutMinutes = 30);

        /// <summary>
        /// Lấy số lượng user online trong workspace.
        /// </summary>
        Task<ApiResult> GetOnlineCountAsync(int workspaceId);

        /// <summary>
        /// Lấy hoạt động realtime của workspace.
        /// </summary>
        Task<ApiResult> GetRealtimeActivityAsync(int workspaceId);
    }
}
