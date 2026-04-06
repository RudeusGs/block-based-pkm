using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskPerformanceMetric: Thực thể quản lý và tính toán hiệu suất công việc.
    /// Thiết kế theo Rich Domain Model để đảm bảo tính nhất quán của dữ liệu thống kê.
    /// </summary>
    public class TaskPerformanceMetric : EntityBase
    {
        // Định danh
        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // Chỉ số số lượng
        public int CompletionCount { get; private set; }
        public int AbandonedCount { get; private set; }
        public decimal CompletionRate { get; private set; }

        // Chỉ số thời gian
        public double AverageDurationMinutes { get; private set; }
        public int? MinDurationMinutes { get; private set; }
        public int? MaxDurationMinutes { get; private set; }

        // Chỉ số thói quen
        public int? MostCommonCompletionHour { get; private set; }
        public int? MostCommonCompletionDayOfWeek { get; private set; }
        public DateTime? LastCompletedAt { get; private set; }

        /// <summary>
        /// Số ngày kể từ lần hoàn thành cuối cùng. 
        /// Không nên lưu cứng vào DB vì nó thay đổi theo thời gian thực (Computed Property).
        /// </summary>
        public int? DaysSinceLastCompletion => LastCompletedAt.HasValue
            ? (DateTime.UtcNow - LastCompletedAt.Value).Days
            : null;

        public string? CompletionTrend { get; private set; } // JSON: {"last_7_days": 5, ...}
        public DateTime LastCalculatedAt { get; private set; }

        protected TaskPerformanceMetric() { }

        public TaskPerformanceMetric(int taskId, int userId, int workspaceId)
        {
            if (taskId <= 0 || userId <= 0 || workspaceId <= 0)
                throw new DomainException("Thông tin định danh Metric không hợp lệ.");

            TaskId = taskId;
            UserId = userId;
            WorkspaceId = workspaceId;

            CompletionCount = 0;
            AbandonedCount = 0;
            CompletionRate = 0;
            AverageDurationMinutes = 0;
            LastCalculatedAt = DateTime.UtcNow;
        }


        /// <summary>
        /// Ghi nhận một lần hoàn thành task và cập nhật toàn bộ metrics.
        /// </summary>
        public void RecordCompletion(int durationMinutes, DateTime completedAt)
        {
            if (completedAt > DateTime.UtcNow)
                throw new DomainException("Thời gian hoàn thành không thể nằm trong tương lai.");

            // 1. Cập nhật số lượng & Thời điểm cuối
            CompletionCount++;
            LastCompletedAt = completedAt;

            // 2. Cập nhật thói quen (AI sẽ dùng cái này để biết bạn thường làm việc lúc mấy giờ)
            MostCommonCompletionHour = completedAt.Hour;
            MostCommonCompletionDayOfWeek = (int)completedAt.DayOfWeek;

            // 3. Tính toán thời gian (Nên tính trước khi tính Rate để đảm bảo thứ tự logic)
            UpdateDurationMetrics(durationMinutes);

            // 4. Tính toán tỷ lệ
            UpdateCompletionRate();

            LastCalculatedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Ghi nhận việc bỏ dở task.
        /// </summary>
        public void RecordAbandonment()
        {
            AbandonedCount++;
            UpdateCompletionRate();
            LastCalculatedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Cập nhật xu hướng hoàn thành (Thường gọi từ một Background Job phân tích dữ liệu lớn).
        /// </summary>
        public void UpdateTrend(string jsonTrend)
        {
            CompletionTrend = jsonTrend;
            LastCalculatedAt = DateTime.UtcNow;
            MarkUpdated();
        }


        private void UpdateCompletionRate()
        {
            int totalAttempts = CompletionCount + AbandonedCount;
            if (totalAttempts == 0) return;
            CompletionRate = Math.Round((decimal)CompletionCount / totalAttempts * 100, 2);
        }

        private void UpdateDurationMetrics(int duration)
        {
            // Tránh dữ liệu rác (duration âm)
            int validDuration = Math.Max(0, duration);

            // Công thức tính trung bình trượt (Moving Average) chính xác:
            // NewAvg = ((OldAvg * (N-1)) + NewVal) / N
            AverageDurationMinutes = Math.Round(
                ((AverageDurationMinutes * (CompletionCount - 1)) + validDuration) / CompletionCount, 2);

            // Cập nhật kỷ lục nhanh nhất/chậm nhất
            if (!MinDurationMinutes.HasValue || validDuration < MinDurationMinutes)
                MinDurationMinutes = validDuration;

            if (!MaxDurationMinutes.HasValue || validDuration > MaxDurationMinutes)
                MaxDurationMinutes = validDuration;
        }
    }
}