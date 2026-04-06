using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskRecommendation: Thực thể quản lý vòng đời của một lời gợi ý từ hệ thống.
    /// Đóng gói logic chuyển đổi trạng thái và bảo vệ tính toàn vẹn của điểm số.
    /// </summary>
    public class TaskRecommendation : EntityBase
    {
        // Định danh
        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // Dữ liệu Gợi ý
        public decimal RecommendationScore { get; private set; }
        public string? RecommendationReason { get; private set; }
        public DateTime RecommendedAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        // Trạng thái & Phản hồi từ User
        public StatusTaskRecommendation Status { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }

        protected TaskRecommendation() { }

        /// <summary>
        /// Khởi tạo một lời gợi ý mới từ hệ thống.
        /// </summary>
        public TaskRecommendation(
            int taskId,
            int userId,
            int workspaceId,
            decimal score,
            string? reason,
            int validHours = 24)
        {
            if (taskId <= 0 || userId <= 0 || workspaceId <= 0)
                throw new DomainException("Thông tin định danh gợi ý không hợp lệ.");

            if (score < 0 || score > 100)
                throw new DomainException("Điểm gợi ý phải nằm trong khoảng 0-100.");

            TaskId = taskId;
            UserId = userId;
            WorkspaceId = workspaceId;
            RecommendationScore = score;
            RecommendationReason = reason;

            RecommendedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddHours(validHours);
            Status = StatusTaskRecommendation.Pending;
        }


        /// <summary>
        /// Chấp nhận gợi ý (User chọn làm task này).
        /// </summary>
        public void Accept()
        {
            EnsureNotExpired();
            if (Status != StatusTaskRecommendation.Pending)
                throw new DomainException("Chỉ có thể chấp nhận gợi ý đang ở trạng thái chờ.");

            Status = StatusTaskRecommendation.Accepted;
            AcceptedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Từ chối gợi ý (User không muốn làm task này).
        /// </summary>
        public void Reject()
        {
            if (Status != StatusTaskRecommendation.Pending && Status != StatusTaskRecommendation.Accepted)
                throw new DomainException("Không thể từ chối gợi ý ở trạng thái hiện tại.");

            Status = StatusTaskRecommendation.Rejected;
            RejectedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Đánh dấu đã hoàn thành task thông qua lời gợi ý này.
        /// </summary>
        public void MarkCompleted()
        {
            if (Status != StatusTaskRecommendation.Accepted)
                throw new DomainException("Task phải được chấp nhận trước khi đánh dấu hoàn thành.");

            Status = StatusTaskRecommendation.Completed;
            MarkUpdated();
        }

        /// <summary>
        /// Kiểm tra và cập nhật trạng thái hết hạn.
        /// </summary>
        public void CheckExpiration()
        {
            if (Status == StatusTaskRecommendation.Pending && DateTime.UtcNow > ExpiresAt)
            {
                Status = StatusTaskRecommendation.Expired;
                MarkUpdated();
            }
        }

        private void EnsureNotExpired()
        {
            if (Status == StatusTaskRecommendation.Expired || (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt))
            {
                Status = StatusTaskRecommendation.Expired;
                throw new DomainException("Gợi ý này đã hết hạn.");
            }
        }
    }
}