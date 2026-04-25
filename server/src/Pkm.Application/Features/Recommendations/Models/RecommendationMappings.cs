using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Models;

public static class RecommendationMappings
{
    public static UserTaskPreferenceDto ToDto(this UserTaskPreference preference)
        => new(
            preference.Id,
            preference.UserId,
            preference.WorkspaceId,
            preference.WorkDayStartHour,
            preference.WorkDayEndHour,
            preference.PreferredDaysOfWeek.ToArray(),
            preference.MaxRecommendationsPerSession,
            preference.MinPriorityForRecommendation,
            preference.RecommendationSensitivity,
            preference.RecommendationIntervalMinutes,
            preference.EnableAutoRecommendation,
            preference.CreatedDate,
            preference.UpdatedDate);

    public static TaskRecommendationDto ToDto(
        this TaskRecommendation recommendation,
        RecommendationCandidateReadModel? task = null)
        => new(
            recommendation.Id,
            recommendation.TaskId,
            recommendation.UserId,
            recommendation.WorkspaceId,
            task?.PageId,
            task?.Title ?? string.Empty,
            task?.Description,
            task?.Priority ?? default,
            task?.Status ?? default,
            task?.DueDate,
            recommendation.Score,
            recommendation.Reason,
            recommendation.Status,
            recommendation.ExpiresAt,
            recommendation.AcceptedAt,
            recommendation.RejectedAt,
            recommendation.CompletedAt,
            recommendation.CreatedDate,
            recommendation.UpdatedDate);
}