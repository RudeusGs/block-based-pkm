using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskPreference: Tùy chỉnh cá nhân cho gợi ý task của user.
    /// Lưu những thói quen, giờ làm việc, độ ưu tiên của user để tùy chỉnh thuật toán.
    /// </summary>
    public class UserTaskPreference : EntityBase
    {
        /// <summary>
        /// Mã định danh của user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Mã định danh của workspace.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Giờ bắt đầu ngày làm việc (0-23).
        /// Ví dụ: 9 = người dùng thường bắt đầu làm việc lúc 9 giờ sáng.
        /// </summary>
        public int WorkDayStartHour { get; set; } = 8;

        /// <summary>
        /// Giờ kết thúc ngày làm việc (0-23).
        /// Ví dụ: 18 = kết thúc lúc 6 giờ chiều.
        /// </summary>
        public int WorkDayEndHour { get; set; } = 18;

        /// <summary>
        /// Tối đa bao nhiêu task được gợi ý cùng một lúc (0-10).
        /// </summary>
        public int MaxRecommendationsPerSession { get; set; } = 3;

        /// <summary>
        /// Mức độ ưu tiên tối thiểu cho gợi ý (Low, Medium, High).
        /// Chỉ gợi ý task có mức độ ưu tiên >= giá trị này.
        /// </summary>
        public string MinPriorityForRecommendation { get; set; } = "Low";

        /// <summary>
        /// Có bật tính năng gợi ý tự động hay không.
        /// </summary>
        public bool EnableAutoRecommendation { get; set; } = true;

        /// <summary>
        /// Khoảng cách tối thiểu giữa các gợi ý (tính bằng phút). 30 phút
        /// </summary>
        public int RecommendationIntervalMinutes { get; set; } = 30;

        /// <summary>
        /// Những ngày trong tuần nào user muốn nhận gợi ý (JSON format).
        /// Ví dụ: [1,2,3,4,5] = Monday-Friday (0=Sunday, 1=Monday, ..., 6=Saturday)
        /// </summary>
        public string? PreferredDaysOfWeek { get; set; } = "[1,2,3,4,5]";

        /// <summary>
        /// Độ nhạy của thuật toán gợi ý (0-100).
        /// - 0-30: Tương đối, chỉ gợi ý những task rất phù hợp
        /// - 31-70: Trung bình, gợi ý task trung bình hoặc hơn
        /// - 71-100: Tích cực, gợi ý nhiều task để user chọn
        /// </summary>
        public int RecommendationSensitivity { get; set; } = 50;
    }
}
