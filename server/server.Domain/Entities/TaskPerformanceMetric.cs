using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskPerformanceMetric: Chỉ số hiệu suất của task.
    /// Lưu những metrics tổng hợp: tỷ lệ hoàn thành, thời gian trung bình, trend, v.v.
    /// Dùng để tính toán trọng số gợi ý hiệu quả hơn.
    /// </summary>
    public class TaskPerformanceMetric : EntityBase
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
        /// Mã định danh của workspace.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Số lần user đã hoàn thành task.
        /// </summary>
        public int CompletionCount { get; set; } = 0;

        /// <summary>
        /// Số lần user đã bắt đầu nhưng không hoàn thành task.
        /// </summary>
        public int AbandonedCount { get; set; } = 0;

        /// <summary>
        /// Tỷ lệ hoàn thành (%).
        /// Công thức: (CompletionCount / (CompletionCount + AbandonedCount)) * 100
        /// </summary>
        public decimal CompletionRate { get; set; } = 0;

        /// <summary>
        /// Thời gian hoàn thành trung bình (phút).
        /// </summary>
        public double AverageDurationMinutes { get; set; } = 0;

        /// <summary>
        /// Thời gian hoàn thành nhanh nhất (phút).
        /// </summary>
        public int? MinDurationMinutes { get; set; }

        /// <summary>
        /// Thời gian hoàn thành chậm nhất (phút).
        /// </summary>
        public int? MaxDurationMinutes { get; set; }

        /// <summary>
        /// Giờ trong ngày user thường hoàn thành task này (0-23).
        /// Tính dựa trên lịch sử: giờ nào user hoàn thành task nhiều nhất.
        /// </summary>
        public int? MostCommonCompletionHour { get; set; }

        /// <summary>
        /// Ngày trong tuần user thường hoàn thành task này (0-6, 0=Sunday).
        /// </summary>
        public int? MostCommonCompletionDayOfWeek { get; set; }

        /// <summary>
        /// Số ngày kể từ lần hoàn thành gần nhất.
        /// Dùng để tính mức độ cần gợi ý lại task (nếu lâu mà không làm).
        /// </summary>
        public int? DaysSinceLastCompletion { get; set; }

        /// <summary>
        /// Trend hoàn thành (JSON format):
        /// Lưu tần suất hoàn thành trong các khoảng thời gian gần đây (1 tuần, 2 tuần, 1 tháng).
        /// Ví dụ: {"last_week": 3, "last_2_weeks": 5, "last_month": 8}
        /// </summary>
        public string? CompletionTrend { get; set; }

        /// <summary>
        /// Thời điểm cập nhật metrics lần cuối.
        /// </summary>
        public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
