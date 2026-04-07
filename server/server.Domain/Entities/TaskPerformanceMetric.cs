using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskPerformanceMetric: "Bộ não" phân tích hiệu suất của Task.
    /// Chứa logic tính toán chuyên sâu để phục vụ thuật toán gợi ý và báo cáo.
    /// </summary>
    public class TaskPerformanceMetric : EntityBase
    {
        // --- Định danh ---
        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // --- Chỉ số số lượng & Tỷ lệ ---
        public int CompletionCount { get; private set; }
        public int AbandonedCount { get; private set; }
        public decimal CompletionRate { get; private set; }

        // --- Chỉ số thời gian (Phút) ---
        public double AverageDurationMinutes { get; private set; }
        public int? MinDurationMinutes { get; private set; }
        public int? MaxDurationMinutes { get; private set; }

        // --- Chỉ số thói quen (AI Core) ---
        // Giờ hoàn thành trung bình (Dùng Cumulative Moving Average)
        public double AverageCompletionHour { get; private set; }
        public int? MostCommonCompletionDayOfWeek { get; private set; }
        public DateTime? LastCompletedAt { get; private set; }

        // --- Dữ liệu xu hướng & Phân tích lớn ---
        public string? CompletionTrend { get; private set; } // JSON: {"last_7_days": 5, "streak": 3}
        public DateTime LastCalculatedAt { get; private set; }

        /// <summary>
        /// Computed Property: Số ngày kể từ lần cuối hoàn thành.
        /// </summary>
        public int? DaysSinceLastCompletion => LastCompletedAt.HasValue
            ? (DateTime.UtcNow - LastCompletedAt.Value).Days
            : null;

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
            AverageCompletionHour = 0;
            LastCalculatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ghi nhận một lần hoàn thành task và tái tính toán toàn bộ chỉ số.
        /// </summary>
        public void RecordCompletion(int durationMinutes, DateTime completedAt)
        {
            if (completedAt > DateTime.UtcNow)
                throw new DomainException("Thời gian hoàn thành không thể nằm trong tương lai.");

            CompletionCount++;
            LastCompletedAt = completedAt;

            // 1. Cập nhật thói quen thời gian (CMA)
            UpdateHourMetric(completedAt.Hour);
            MostCommonCompletionDayOfWeek = (int)completedAt.DayOfWeek;

            // 2. Cập nhật chỉ số thời gian thực hiện
            UpdateDurationMetrics(durationMinutes);

            // 3. Cập nhật tỷ lệ thành công
            UpdateCompletionRate();

            LastCalculatedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Ghi nhận việc bỏ dở task (User bắt đầu làm nhưng không hoàn thành).
        /// </summary>
        public void RecordAbandonment()
        {
            AbandonedCount++;
            UpdateCompletionRate();
            LastCalculatedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        /// <summary>
        /// Cập nhật dữ liệu xu hướng từ kết quả phân tích của Background Job/AI.
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

            // Tính phần trăm hoàn thành trên tổng số lần thử
            CompletionRate = Math.Round((decimal)CompletionCount / totalAttempts * 100, 2);
        }

        private void UpdateDurationMetrics(int duration)
        {
            int validDuration = Math.Max(1, duration); // Tối thiểu 1 phút

            // Công thức Cumulative Moving Average cho thời gian thực hiện
            AverageDurationMinutes = Math.Round(
                ((AverageDurationMinutes * (CompletionCount - 1)) + validDuration) / CompletionCount, 2);

            if (!MinDurationMinutes.HasValue || validDuration < MinDurationMinutes)
                MinDurationMinutes = validDuration;

            if (!MaxDurationMinutes.HasValue || validDuration > MaxDurationMinutes)
                MaxDurationMinutes = validDuration;
        }

        private void UpdateHourMetric(int currentHour)
        {
            // Sử dụng CMA để tìm ra "giờ vàng" mà người dùng thường hoàn thành task này
            if (CompletionCount <= 1)
            {
                AverageCompletionHour = currentHour;
            }
            else
            {
                AverageCompletionHour = Math.Round(
                    ((AverageCompletionHour * (CompletionCount - 1)) + currentHour) / CompletionCount, 2);
            }
        }
    }
}