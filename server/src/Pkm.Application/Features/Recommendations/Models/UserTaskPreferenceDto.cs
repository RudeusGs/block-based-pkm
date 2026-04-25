using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Models;

public sealed record UserTaskPreferenceDto(
    Guid Id,
    Guid UserId,
    Guid WorkspaceId,
    int WorkDayStartHour,
    int WorkDayEndHour,
    IReadOnlyCollection<int> PreferredDaysOfWeek,
    int MaxRecommendationsPerSession,
    PriorityWorkTask MinPriorityForRecommendation,
    int RecommendationSensitivity,
    int RecommendationIntervalMinutes,
    bool EnableAutoRecommendation,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);