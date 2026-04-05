using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskHistory: Lịch sử hoàn thành task của user.
    /// Dùng để tracking: bao lâu user hoàn thành task, bao giờ hoàn thành, để tính toán trọng số gợi ý.
    /// </summary>
    public class UserTaskHistory : EntityBase
    {
        /// <summary>
        /// Mã định danh của task.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Mã định danh của user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Thời điểm bắt đầu làm task.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Thời điểm hoàn thành task.
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Thời gian dành để hoàn thành task (tính bằng phút).
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Trạng thái hoàn thành (Completed, Abandoned, Skipped, ...).
        /// </summary>
        public string Status { get; set; } = "Completed";

        /// <summary>
        /// Ghi chú hoặc bình luận khi hoàn thành.
        /// </summary>
        public string? Notes { get; set; }
    }
}
