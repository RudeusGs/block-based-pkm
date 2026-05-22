using Pkm.Application.Features.Activity.Models;
using Pkm.Domain.Audit;

namespace Pkm.Application.Abstractions.Persistence;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLogDto>> ListByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        CancellationToken cancellationToken = default);

    void Add(ActivityLog activityLog);
}
