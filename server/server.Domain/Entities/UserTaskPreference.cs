using System.Text.Json;
using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskPreference: Cấu hình cá nhân hóa cho hệ thống gợi ý.
    /// Chứa các quy tắc chặn (Guard Clauses) để điều tiết tần suất và thời điểm gợi ý.
    /// </summary>
    public class UserTaskPreference : EntityBase
    {
        // Định danh
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // Cấu hình thời gian làm việc
        public int WorkDayStartHour { get; private set; }
        public int WorkDayEndHour { get; private set; }
        public string? PreferredDaysOfWeek { get; private set; } // Lưu JSON: [1,2,3,4,5]

        // Tham số thuật toán
        public int MaxRecommendationsPerSession { get; private set; }
        public StatusUserTaskPreference MinPriorityForRecommendation { get; private set; }
        public int RecommendationSensitivity { get; private set; }
        public int RecommendationIntervalMinutes { get; private set; }
        public bool EnableAutoRecommendation { get; private set; }

        protected UserTaskPreference() { }

        public UserTaskPreference(int userId, int workspaceId)
        {
            if (userId <= 0 || workspaceId <= 0)
                throw new DomainException("UserId và WorkspaceId không hợp lệ.");

            UserId = userId;
            WorkspaceId = workspaceId;

            WorkDayStartHour = 8;
            WorkDayEndHour = 18;
            MaxRecommendationsPerSession = 3;
            RecommendationSensitivity = 50;
            RecommendationIntervalMinutes = 30;
            EnableAutoRecommendation = true;
            PreferredDaysOfWeek = "[1,2,3,4,5]";
        }

        /// <summary>
        /// Cập nhật khung giờ làm việc.
        /// </summary>
        public void UpdateWorkHours(int start, int end)
        {
            if (start < 0 || start > 23 || end < 0 || end > 23)
                throw new DomainException("Giờ làm việc phải từ 0 đến 23.");

            if (start >= end)
                throw new DomainException("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");

            WorkDayStartHour = start;
            WorkDayEndHour = end;
            MarkUpdated();
        }

        /// <summary>
        /// Cập nhật độ nhạy của thuật toán.
        /// </summary>
        public void UpdateSensitivity(int sensitivity)
        {
            if (sensitivity < 0 || sensitivity > 100)
                throw new DomainException("Độ nhạy phải nằm trong khoảng 0-100.");

            RecommendationSensitivity = sensitivity;
            MarkUpdated();
        }

        /// <summary>
        /// Kiểm tra xem hiện tại có phải là thời điểm thích hợp để gợi ý không.
        /// </summary>
        public bool IsSuitableForRecommendation(DateTime currentTime)
        {
            if (!EnableAutoRecommendation) return false;

            // 1. Kiểm tra giờ làm việc
            if (currentTime.Hour < WorkDayStartHour || currentTime.Hour >= WorkDayEndHour)
                return false;

            // 2. Kiểm tra ngày làm việc (Parse JSON đơn giản)
            var dayOfWeek = (int)currentTime.DayOfWeek;
            if (PreferredDaysOfWeek != null && !PreferredDaysOfWeek.Contains(dayOfWeek.ToString()))
                return false;

            return true;
        }

        /// <summary>
        /// Cập nhật danh sách ngày làm việc ưu tiên.
        /// </summary>
        public void SetPreferredDays(List<int> days)
        {
            if (days == null || !days.Any())
                throw new DomainException("Danh sách ngày ưu tiên không được để trống.");

            if (days.Any(d => d < 0 || d > 6))
                throw new DomainException("Giá trị ngày không hợp lệ (0-6).");

            PreferredDaysOfWeek = JsonSerializer.Serialize(days.Distinct().OrderBy(d => d));
            MarkUpdated();
        }

        public void SetAutoRecommendation(bool enable)
        {
            EnableAutoRecommendation = enable;
            MarkUpdated();
        }
    }
}