using Pkm.Application.Features.Recommendations.Models;

namespace Pkm.Api.Contracts.Responses.Recommendations;

public static class RecommendationResponseMappings
{
    public static TaskRecommendationResponse ToResponse(this TaskRecommendationDto dto)
        => new(
            dto.Id,
            dto.TaskId,
            dto.UserId,
            dto.WorkspaceId,
            dto.PageId,
            dto.TaskTitle,
            dto.TaskDescription,
            dto.TaskPriority.ToString(),
            dto.TaskStatus.ToString(),
            dto.TaskDueDate,
            dto.Score,
            dto.Reason,
            dto.Status.ToString(),
            dto.ExpiresAt,
            dto.AcceptedAt,
            dto.RejectedAt,
            dto.CompletedAt,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static TaskRecommendationPagedResultResponse ToResponse(this TaskRecommendationPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);

    public static UserTaskPreferenceResponse ToResponse(this UserTaskPreferenceDto dto)
        => new(
            dto.Id,
            dto.UserId,
            dto.WorkspaceId,
            dto.WorkDayStartHour,
            dto.WorkDayEndHour,
            dto.PreferredDaysOfWeek,
            dto.MaxRecommendationsPerSession,
            dto.MinPriorityForRecommendation.ToString(),
            dto.RecommendationSensitivity,
            dto.RecommendationIntervalMinutes,
            dto.EnableAutoRecommendation,
            dto.CreatedDate,
            dto.UpdatedDate);
}