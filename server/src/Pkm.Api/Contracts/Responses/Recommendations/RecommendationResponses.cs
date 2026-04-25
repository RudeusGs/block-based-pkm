namespace Pkm.Api.Contracts.Responses.Recommendations;

public sealed record TaskRecommendationResponse(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    string? TaskDescription,
    string TaskPriority,
    string TaskStatus,
    DateTimeOffset? TaskDueDate,
    decimal Score,
    string? Reason,
    string Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record TaskRecommendationPagedResultResponse(
    IReadOnlyList<TaskRecommendationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record UserTaskPreferenceResponse(
    Guid Id,
    Guid UserId,
    Guid WorkspaceId,
    int WorkDayStartHour,
    int WorkDayEndHour,
    IReadOnlyCollection<int> PreferredDaysOfWeek,
    int MaxRecommendationsPerSession,
    string MinPriorityForRecommendation,
    int RecommendationSensitivity,
    int RecommendationIntervalMinutes,
    bool EnableAutoRecommendation,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);