using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// RealtimeSession: Thực thể quản lý phiên kết nối realtime của user.
    /// 
    ///  Mục đích:
    /// - Theo dõi trạng thái online của user trong hệ thống
    /// - Hỗ trợ các tính năng realtime (presence, collaboration, cursor...)
    /// 
    ///  Lưu ý:
    /// - 1 User có thể có nhiều session (multi-device, multi-tab)
    /// - Session được xác định bằng ConnectionId (SignalR)
    /// 
    ///  Design Principle:
    /// - Lightweight entity (chỉ tracking state)
    /// - Không chứa business logic phức tạp
    /// </summary>
    public class RealtimeSession : EntityBase
    {
        /// <summary>
        /// User sở hữu session
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Workspace hiện tại
        /// </summary>
        public int WorkspaceId { get; private set; }

        /// <summary>
        /// Page hiện tại (có thể null)
        /// </summary>
        public int? PageId { get; private set; }

        /// <summary>
        /// ID kết nối realtime (SignalR/WebSocket)
        /// </summary>
        public string ConnectionId { get; private set; }

        /// <summary>
        /// Thời điểm bắt đầu kết nối
        /// </summary>
        public DateTime ConnectedAt { get; private set; }

        /// <summary>
        /// Thời điểm ping cuối cùng (heartbeat)
        /// </summary>
        public DateTime LastPing { get; private set; }

        protected RealtimeSession() { }

        /// <summary>
        /// Khởi tạo session mới khi user connect
        /// </summary>
        public RealtimeSession(int userId, int workspaceId, string connectionId, int? pageId = null)
        {
            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(connectionId))
                throw new DomainException("ConnectionId không hợp lệ.");

            UserId = userId;
            WorkspaceId = workspaceId;
            PageId = pageId;
            ConnectionId = connectionId;

            ConnectedAt = DateTime.UtcNow;
            LastPing = DateTime.UtcNow;
        }

        /// <summary>
        /// Cập nhật heartbeat (keep-alive)
        /// </summary>
        public void Ping()
        {
            LastPing = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// User chuyển sang page khác
        /// </summary>
        public void MoveToPage(int? newPageId)
        {
            if (PageId == newPageId) return;

            PageId = newPageId;
            Ping();
        }

        /// <summary>
        /// Kiểm tra session có hết hạn không
        /// </summary>
        /// <param name="timeoutSeconds">Timeout (default: 60s)</param>
        public bool IsExpired(int timeoutSeconds = 60)
        {
            return (DateTime.UtcNow - LastPing).TotalSeconds > timeoutSeconds;
        }

        /// <summary>
        /// User chuyển workspace
        /// </summary>
        public void SwitchWorkspace(int newWorkspaceId, int? newPageId = null)
        {
            if (newWorkspaceId <= 0)
                throw new DomainException("WorkspaceId không hợp lệ.");

            WorkspaceId = newWorkspaceId;
            PageId = newPageId;

            Ping();
        }

        /// <summary>
        /// Update connectionId (reconnect case)
        /// </summary>
        public void UpdateConnection(string newConnectionId)
        {
            if (string.IsNullOrWhiteSpace(newConnectionId))
                throw new DomainException("ConnectionId không hợp lệ.");

            ConnectionId = newConnectionId;
            Ping();
        }
    }
}