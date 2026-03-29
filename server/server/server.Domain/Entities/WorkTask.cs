using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkTask: Công việc cần làm trong một workspace hoặc page.
    /// </summary>
    public class WorkTask : EntityBase
    {
        /// <summary>
        /// Mã định danh của trang.
        /// </summary>
        public int? PageId { get; set; }

        /// <summary>
        /// Mã định danh của không gian làm việc.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Tên đầu việc cần làm.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Nội dung chi tiết hoặc yêu cầu của công việc.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái của công việc (To Do, Doing, Done, ...).
        /// </summary>
        public string Status { get; set; } = "To Do";

        /// <summary>
        /// Mức độ ưu tiên (Low, Medium, High).
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Hạn chót phải hoàn thành.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Mã định danh của người tạo ra thẻ công việc.
        /// </summary>
        public int CreatedById { get; set; }

        // ========== Properties hỗ trợ thuật toán gợi ý ==========

        /// <summary>
        /// Số lần task được hoàn thành.
        /// </summary>
        public int CompletionCount { get; set; } = 0;

        /// <summary>
        /// Thời điểm cuối cùng task được hoàn thành.
        /// </summary>
        public DateTime? LastCompletedAt { get; set; }

        /// <summary>
        /// Tổng thời gian (tính bằng phút) mà user đã dành để hoàn thành task này.
        /// </summary>
        public int TotalDurationMinutes { get; set; } = 0;

        /// <summary>
        /// Trọng số/điểm của task (dùng cho thuật toán gợi ý).
        /// Tính toán dựa trên: tần suất, thời gian hoàn thành, độ ưu tiên, ...
        /// </summary>
        public decimal RecommendationWeight { get; set; } = 0;

        /// <summary>
        /// Giờ tối ưu nhất trong ngày để làm task này (0-23).
        /// Ví dụ: task này thường được làm lúc 9 giờ sáng, giá trị là 9.
        /// Null = chưa có dữ liệu hoặc không có giờ cụ thể.
        /// </summary>
        public int? OptimalHourOfDay { get; set; }

        /// <summary>
        /// Lịch sử thực hiện gần đây (JSON format): 
        /// Lưu thông tin các lần hoàn thành gần nhất (thời gian, duration) để tính trend.
        /// </summary>
        public string? RecentCompletionHistory { get; set; }
    }
}
