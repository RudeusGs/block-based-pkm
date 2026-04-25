namespace Pkm.Api.Contracts.Requests.Recommendations;

public sealed record UpdateUserTaskPreferenceRequest(
    int WorkDayStartHour,
    int WorkDayEndHour,
    IReadOnlyList<int> PreferredDaysOfWeek,
    int MaxRecommendationsPerSession,
    string MinPriorityForRecommendation,
    int RecommendationSensitivity,
    int RecommendationIntervalMinutes,
    bool EnableAutoRecommendation);