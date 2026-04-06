using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// RealtimeSession: Thực thể quản lý phiên làm việc trực tuyến.
    /// </summary>
    public class RealtimeSession : EntityBase
    {
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }
        public int? PageId { get; private set; }
        public DateTime ConnectedAt { get; private set; }
        public DateTime LastPing { get; private set; }
        protected RealtimeSession() { }

        /// <summary>
        /// Khởi tạo một phiên làm việc mới khi người dùng kết nối
        /// </summary>
        public RealtimeSession(int userId, int workspaceId, int? pageId = null)
        {
            if (userId <= 0) throw new DomainException("UserId không hợp lệ.");
            if (workspaceId <= 0) throw new DomainException("WorkspaceId không hợp lệ.");

            UserId = userId;
            WorkspaceId = workspaceId;
            PageId = pageId;

            ConnectedAt = DateTime.UtcNow;
            LastPing = DateTime.UtcNow;
        }

        /// <summary>
        /// Cập nhật thời điểm hoạt động cuối cùng (Keep-alive).
        /// </summary>
        public void Ping()
        {
            LastPing = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Cập nhật vị trí hiện tại của người dùng khi họ chuyển trang bên trong Workspace.
        /// </summary>
        public void MoveToPage(int? newPageId)
        {
            if (PageId == newPageId) return;

            PageId = newPageId;
            Ping();
        }

        /// <summary>
        /// Kiểm tra xem phiên này còn hoạt động hay đã quá hạn (Timeout).
        /// </summary>
        /// <param name="timeoutSeconds">Thời gian chờ tối đa (giây)</param>
        public bool IsExpired(int timeoutSeconds = 60)
        {
            return (DateTime.UtcNow - LastPing).TotalSeconds > timeoutSeconds;
        }

        /// <summary>
        /// Thay đổi Workspace (Nếu người dùng switch workspace mà không ngắt kết nối).
        /// </summary>
        public void SwitchWorkspace(int newWorkspaceId, int? newPageId = null)
        {
            if (newWorkspaceId <= 0) throw new DomainException("WorkspaceId mới không hợp lệ.");

            WorkspaceId = newWorkspaceId;
            PageId = newPageId;
            Ping();
        }
    }
}