using Pkm.Application.Features.Activity.Models;
using Pkm.Domain.Audit;

namespace Pkm.Application.Abstractions.Persistence;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLogDto>> ListByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        CancellationToken cancellationToken = default);

    void Add(ActivityLog activityLog);
}
