using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskHistory: Nhật ký chi tiết tương tác của User với Task.
    /// Đóng gói logic tính toán thời gian thực tế và quản lý trạng thái lịch sử.
    /// </summary>
    public class UserTaskHistory : EntityBase
    {
        // Định danh
        public int TaskId { get; private set; }
        public int UserId { get; private set; }

        // Dữ liệu thời gian
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public int DurationMinutes { get; private set; }

        // Trạng thái & Ghi chú
        public StatusUserTaskHistory Status { get; private set; }
        public string? Notes { get; private set; }

        protected UserTaskHistory() { }

        /// <summary>
        /// Khởi tạo một bản ghi lịch sử khi User bắt đầu thực hiện Task.
        /// </summary>
        public UserTaskHistory(int taskId, int userId)
        {
            if (taskId <= 0 || userId <= 0)
                throw new DomainException("TaskId và UserId phải lớn hơn 0.");

            TaskId = taskId;
            UserId = userId;
            StartedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Hoàn thành Task và tự động tính toán thời gian thực hiện.
        /// </summary>
        public void MarkAsCompleted(string? notes = null)
        {
            if (Status == StatusUserTaskHistory.Completed) return;

            CompletedAt = DateTime.UtcNow;
            Status = StatusUserTaskHistory.Completed;
            Notes = notes?.Trim();
            CalculateDuration();
            MarkUpdated();
        }

        /// <summary>
        /// Ghi nhận việc từ bỏ Task giữa chừng.
        /// </summary>
        public void MarkAsAbandoned(string? notes = null)
        {
            Status = StatusUserTaskHistory.Abandoned;
            CompletedAt = DateTime.UtcNow;
            Notes = notes?.Trim();

            CalculateDuration();
            MarkUpdated();
        }

        /// <summary>
        /// Ghi nhận việc bỏ qua Task (Skipped).
        /// Thường Duration sẽ bằng 0 vì User chưa thực sự bắt đầu làm.
        /// </summary>
        public void MarkAsSkipped()
        {
            Status = StatusUserTaskHistory.Skipped;
            DurationMinutes = 0;
            MarkUpdated();
        }

        private void CalculateDuration()
        {
            if (!CompletedAt.HasValue) return;

            var diff = CompletedAt.Value - StartedAt;
            DurationMinutes = diff.TotalMinutes < 1 ? 1 : (int)Math.Round(diff.TotalMinutes);
        }
    }
}