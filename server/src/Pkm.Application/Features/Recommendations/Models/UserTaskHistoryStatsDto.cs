namespace Pkm.Application.Features.Recommendations.Models;

public sealed record UserTaskHistoryStatsDto(
    Guid UserId,
    Guid WorkspaceId,
    int CompletedCount,
    int AbandonedCount,
    int SkippedCount,
    double AverageDurationMinutes,
    IReadOnlyDictionary<Guid, int> CompletedByTaskId,
    IReadOnlyDictionary<Guid, int> SkippedOrAbandonedByTaskId);