using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskRecommendation: Thực thể đại diện cho một đề xuất Task được tạo bởi hệ thống.
    /// 
    ///  Mục đích:
    /// - Lưu trữ kết quả gợi ý (score, reason) từ hệ thống (AI / Service)
    /// - Quản lý vòng đời của recommendation (Pending → Accepted → Completed / Rejected / Expired)
    /// 
    ///  Design Principle:
    /// - Entity chỉ quản lý state và business rule
    /// - Logic AI / Recommendation phải nằm ngoài (Service Layer)
    /// </summary>
    public class TaskRecommendation : EntityBase
    {
        // Định danh
        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // Dữ liệu gợi ý
        public decimal Score { get; private set; }
        public string? Reason { get; private set; }

        public DateTime RecommendedAt { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        // Trạng thái
        public StatusTaskRecommendation Status { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }

        protected TaskRecommendation() { }

        /// <summary>
        /// Khởi tạo một recommendation mới từ hệ thống
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
                throw new DomainException("Thông tin định danh không hợp lệ.");

            if (score < 0 || score > 100)
                throw new DomainException("Score phải nằm trong khoảng 0-100.");

            TaskId = taskId;
            UserId = userId;
            WorkspaceId = workspaceId;

            Score = score;
            Reason = reason;

            RecommendedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddHours(validHours);

            Status = StatusTaskRecommendation.Pending;
        }

        /// <summary>
        /// User chấp nhận recommendation
        /// </summary>
        public void Accept()
        {
            EnsureNotExpired();

            if (Status != StatusTaskRecommendation.Pending)
                throw new DomainException("Chỉ có thể accept khi đang Pending.");

            Status = StatusTaskRecommendation.Accepted;
            AcceptedAt = DateTime.UtcNow;

            MarkUpdated();
        }

        /// <summary>
        /// User từ chối recommendation
        /// </summary>
        public void Reject()
        {
            if (Status != StatusTaskRecommendation.Pending && Status != StatusTaskRecommendation.Accepted)
                throw new DomainException("Không thể reject ở trạng thái hiện tại.");

            Status = StatusTaskRecommendation.Rejected;
            RejectedAt = DateTime.UtcNow;

            MarkUpdated();
        }

        /// <summary>
        /// Đánh dấu task hoàn thành thông qua recommendation
        /// </summary>
        public void MarkCompleted()
        {
            if (Status != StatusTaskRecommendation.Accepted)
                throw new DomainException("Phải accept trước khi complete.");

            Status = StatusTaskRecommendation.Completed;

            MarkUpdated();
        }

        /// <summary>
        /// Kiểm tra hết hạn
        /// </summary>
        public void CheckExpiration()
        {
            if (Status == StatusTaskRecommendation.Pending &&
                ExpiresAt.HasValue &&
                DateTime.UtcNow > ExpiresAt.Value)
            {
                Status = StatusTaskRecommendation.Expired;
                MarkUpdated();
            }
        }

        private void EnsureNotExpired()
        {
            if (Status == StatusTaskRecommendation.Expired ||
                (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value))
            {
                Status = StatusTaskRecommendation.Expired;
                throw new DomainException("Recommendation đã hết hạn.");
            }
        }
    }
}