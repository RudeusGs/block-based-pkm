using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskRecommendation: Gợi ý task cho user tại một thời điểm cụ thể.
    /// Dùng để lưu những task được gợi ý, mức độ phù hợp, và kết quả (user có chọn hay không).
    /// </summary>
    public class TaskRecommendation : EntityBase
    {
        /// <summary>
        /// Mã định danh của task được gợi ý.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Mã định danh của user nhận được gợi ý.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Mã định danh của workspace.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Điểm trọng số gợi ý (0-100).
        /// Càng cao = task càng phù hợp với user.
        /// </summary>
        public decimal RecommendationScore { get; set; }

        /// <summary>
        /// Lý do gợi ý (JSON format):
        /// Chứa thông tin chi tiết: tần suất hoàn thành trước đó, thời gian tối ưu, độ ưu tiên, ...
        /// Ví dụ: {"frequency": "high", "optimal_hour": 9, "priority": "high", "last_completed_days_ago": 3}
        /// </summary>
        public string? RecommendationReason { get; set; }

        /// <summary>
        /// Thời điểm gợi ý.
        /// </summary>
        public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Trạng thái gợi ý (Pending, Accepted, Rejected, Completed, Expired).
        /// - Pending: Vừa gợi ý, chưa user phản hồi
        /// - Accepted: User đã chọn làm task này
        /// - Rejected: User từ chối
        /// - Completed: User đã hoàn thành task
        /// - Expired: Gợi ý hết hạn (quá lâu không làm)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Thời điểm user đã chọn task này (nếu có).
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// Thời điểm hết hạn của gợi ý.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Thời điểm user từ chối gợi ý (nếu có).
        /// </summary>
        public DateTime? RejectedAt { get; set; }
    }
}
