using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Models;

public sealed record TaskRecommendationDto(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    string? TaskDescription,
    PriorityWorkTask TaskPriority,
    StatusWorkTask TaskStatus,
    DateTimeOffset? TaskDueDate,
    decimal Score,
    string? Reason,
    StatusTaskRecommendation Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);