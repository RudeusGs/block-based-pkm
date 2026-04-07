using System.Text.Json;
using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// UserTaskPreference: Cấu hình cá nhân hóa cho hệ thống gợi ý Task.
    /// 
    ///  Mục đích:
    /// - Điều chỉnh hành vi Recommendation Engine theo từng user
    /// - Kiểm soát thời gian, tần suất và điều kiện gợi ý
    /// 
    ///  Lưu ý:
    /// - Entity này KHÔNG chứa logic AI
    /// - Chỉ đóng vai trò config + guard conditions
    /// 
    ///  Design Principle:
    /// - Validate chặt chẽ input
    /// - Tránh string hack (JSON phải parse đúng)
    /// </summary>
    public class UserTaskPreference : EntityBase
    {
        // Định danh
        public int UserId { get; private set; }
        public int WorkspaceId { get; private set; }

        // Work schedule
        public int WorkDayStartHour { get; private set; }
        public int WorkDayEndHour { get; private set; }

        /// <summary>
        /// JSON lưu danh sách ngày trong tuần (0 = Sunday → 6 = Saturday)
        /// </summary>
        public string? PreferredDaysOfWeek { get; private set; }

        // Recommendation config
        public int MaxRecommendationsPerSession { get; private set; }
        public PriorityWorkTask MinPriorityForRecommendation { get; private set; }
        public int RecommendationSensitivity { get; private set; }
        public int RecommendationIntervalMinutes { get; private set; }
        public bool EnableAutoRecommendation { get; private set; }

        protected UserTaskPreference() { }

        public UserTaskPreference(int userId, int workspaceId)
        {
            if (userId <= 0 || workspaceId <= 0)
                throw new DomainException("UserId/WorkspaceId không hợp lệ.");

            UserId = userId;
            WorkspaceId = workspaceId;

            // Default config
            WorkDayStartHour = 8;
            WorkDayEndHour = 18;

            MaxRecommendationsPerSession = 3;
            RecommendationSensitivity = 50;
            RecommendationIntervalMinutes = 30;
            EnableAutoRecommendation = true;

            MinPriorityForRecommendation = PriorityWorkTask.Medium;

            PreferredDaysOfWeek = JsonSerializer.Serialize(new[] { 1, 2, 3, 4, 5 });
        }

        /// <summary>
        /// Update giờ làm việc
        /// </summary>
        public void UpdateWorkHours(int start, int end)
        {
            if (start < 0 || start > 23 || end < 0 || end > 23)
                throw new DomainException("Giờ phải trong khoảng 0-23.");

            if (start >= end)
                throw new DomainException("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");

            WorkDayStartHour = start;
            WorkDayEndHour = end;

            MarkUpdated();
        }

        /// <summary>
        /// Update độ nhạy recommendation
        /// </summary>
        public void UpdateSensitivity(int sensitivity)
        {
            if (sensitivity < 0 || sensitivity > 100)
                throw new DomainException("Độ nhạy phải từ 0-100.");

            RecommendationSensitivity = sensitivity;
            MarkUpdated();
        }

        /// <summary>
        /// Kiểm tra có phù hợp để gợi ý không
        /// </summary>
        public bool IsSuitableForRecommendation(DateTime currentTime)
        {
            if (!EnableAutoRecommendation)
                return false;

            // 1. Check giờ
            if (currentTime.Hour < WorkDayStartHour || currentTime.Hour >= WorkDayEndHour)
                return false;

            // 2. Check ngày
            var allowedDays = ParsePreferredDays();
            var currentDay = (int)currentTime.DayOfWeek;

            if (allowedDays.Any() && !allowedDays.Contains(currentDay))
                return false;

            return true;
        }

        /// <summary>
        /// Set ngày làm việc
        /// </summary>
        public void SetPreferredDays(List<int> days)
        {
            if (days == null || !days.Any())
                throw new DomainException("Danh sách ngày không được rỗng.");

            if (days.Any(d => d < 0 || d > 6))
                throw new DomainException("Ngày phải từ 0-6.");

            PreferredDaysOfWeek = JsonSerializer.Serialize(days.Distinct().OrderBy(d => d));
            MarkUpdated();
        }

        public void SetAutoRecommendation(bool enable)
        {
            EnableAutoRecommendation = enable;
            MarkUpdated();
        }

        /// <summary>
        /// Parse JSON → List<int>
        /// </summary>
        private List<int> ParsePreferredDays()
        {
            if (string.IsNullOrWhiteSpace(PreferredDaysOfWeek))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(PreferredDaysOfWeek) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }
    }
}