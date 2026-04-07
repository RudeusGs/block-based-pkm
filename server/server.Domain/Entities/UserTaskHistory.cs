using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskHistory: Thực thể ghi nhận lịch sử tương tác của User với Task.
    /// 
    /// 🎯 Mục đích:
    /// - Track hành vi: bắt đầu, hoàn thành, bỏ dở, bỏ qua
    /// - Phục vụ analytics, AI recommendation, thống kê hiệu suất
    /// 
    /// ⚠️ Lưu ý:
    /// - Mỗi record là 1 session làm việc
    /// - State chỉ được chuyển theo flow hợp lệ
    /// 
    /// 📌 Flow chuẩn:
    /// Started → Completed / Abandoned / Skipped
    /// (không được đảo ngược)
    /// </summary>
    public class UserTaskHistory : EntityBase
    {
        // --- Định danh ---
        public int TaskId { get; private set; }
        public int UserId { get; private set; }

        // --- Thời gian ---
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public int DurationMinutes { get; private set; }

        // --- Trạng thái ---
        public StatusUserTaskHistory Status { get; private set; }

        // --- Ghi chú ---
        public string? Notes { get; private set; }

        protected UserTaskHistory() { }

        /// <summary>
        /// Bắt đầu một session làm Task
        /// </summary>
        public UserTaskHistory(int taskId, int userId)
        {
            if (taskId <= 0 || userId <= 0)
                throw new DomainException("TaskId/UserId không hợp lệ.");

            TaskId = taskId;
            UserId = userId;

            StartedAt = DateTime.UtcNow;
            Status = StatusUserTaskHistory.InProgress;
        }

        /// <summary>
        /// Hoàn thành Task
        /// </summary>
        public void MarkAsCompleted(string? notes = null)
        {
            EnsureInProgress();

            CompletedAt = DateTime.UtcNow;
            Status = StatusUserTaskHistory.Completed;
            Notes = NormalizeNotes(notes);

            CalculateDuration();
            MarkUpdated();
        }

        /// <summary>
        /// Bỏ dở Task
        /// </summary>
        public void MarkAsAbandoned(string? notes = null)
        {
            EnsureInProgress();

            CompletedAt = DateTime.UtcNow;
            Status = StatusUserTaskHistory.Abandoned;
            Notes = NormalizeNotes(notes);

            CalculateDuration();
            MarkUpdated();
        }

        /// <summary>
        /// Bỏ qua Task (không thực sự làm)
        /// </summary>
        public void MarkAsSkipped()
        {
            EnsureInProgress();

            CompletedAt = DateTime.UtcNow;
            Status = StatusUserTaskHistory.Skipped;
            DurationMinutes = 0;

            MarkUpdated();
        }

        private void CalculateDuration()
        {
            if (!CompletedAt.HasValue) return;

            var diff = CompletedAt.Value - StartedAt;

            if (diff.TotalMinutes < 0)
                throw new DomainException("Thời gian không hợp lệ.");

            DurationMinutes = diff.TotalMinutes < 1
                ? 1
                : (int)Math.Round(diff.TotalMinutes);
        }

        private string? NormalizeNotes(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return null;

            var trimmed = notes.Trim();

            if (trimmed.Length > 1000)
                throw new DomainException("Notes tối đa 1000 ký tự.");

            return trimmed;
        }

        private void EnsureInProgress()
        {
            if (Status != StatusUserTaskHistory.InProgress)
                throw new DomainException("Chỉ có thể thao tác khi đang InProgress.");
        }
    }
}