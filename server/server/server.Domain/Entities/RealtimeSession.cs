using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// RealtimeSession: Phiên làm việc thời gian thực của người dùng.
    /// Theo dõi ai đang trực tuyến, đang xem hoặc chỉnh sửa trang/workspace nào.
    /// </summary>
    public class RealtimeSession : EntityBase
    {
        /// <summary>
        /// Mã định danh của người dùng đang trực tuyến.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Mã định danh của không gian làm việc đang làm việc.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Mã định danh của trang đang xem hoặc chỉnh sửa (có thể null).
        /// </summary>
        public int? PageId { get; set; }

        /// <summary>
        /// Thời điểm bắt đầu kết nối phiên.
        /// </summary>
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm cuối cùng hệ thống ghi nhận người dùng còn hoạt động.
        /// </summary>
        public DateTime LastPing { get; set; } = DateTime.UtcNow;
    }
}
