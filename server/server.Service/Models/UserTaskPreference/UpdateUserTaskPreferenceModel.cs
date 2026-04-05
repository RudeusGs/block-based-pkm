namespace server.Service.Models.UserTaskPreference
{
    public class UpdateUserTaskPreferenceModel
    {
        public int WorkDayStartHour { get; set; }
        public int WorkDayEndHour { get; set; }
        public int MaxRecommendationsPerSession { get; set; }
        public string MinPriorityForRecommendation { get; set; }
        public bool EnableAutoRecommendation { get; set; }
        public int RecommendationIntervalMinutes { get; set; }
        public List<int> PreferredDaysOfWeek { get; set; }
        public int RecommendationSensitivity { get; set; } // 0-100
    }
}
