using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Commands.UpdateUserTaskPreference;

public sealed record UpdateUserTaskPreferenceCommand(
    Guid WorkspaceId,
    int WorkDayStartHour,
    int WorkDayEndHour,
    IReadOnlyList<int> PreferredDaysOfWeek,
    int MaxRecommendationsPerSession,
    PriorityWorkTask MinPriorityForRecommendation,
    int RecommendationSensitivity,
    int RecommendationIntervalMinutes,
    bool EnableAutoRecommendation);