namespace Pkm.Application.Features.Recommendations.Models;

public sealed record UserTaskHistoryStatsDto(
    Guid UserId,
    Guid WorkspaceId,
    int CompletedCount,
    int AbandonedCount,
    int SkippedCount,
    double AverageDurationMinutes,
    int CompletedCreatedByUserCount,
    int CompletedAssignedToUserCount,
    int? MostProductiveHour,
    int? MostProductiveDayOfWeek,
    IReadOnlyDictionary<Guid, int> CompletedByTaskId,
    IReadOnlyDictionary<Guid, int> SkippedOrAbandonedByTaskId)
{
    public int TotalFinishedInteractionCount =>
        CompletedCount + AbandonedCount + SkippedCount;

    public decimal CompletionRatio
    {
        get
        {
            var total = TotalFinishedInteractionCount;
            if (total <= 0) return 0m;

            return CompletedCount / (decimal)total;
        }
    }

    public bool HasEnoughPersonalSignal => TotalFinishedInteractionCount >= 3;
}