using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IUserTaskHistoryRepository
{
    Task<IReadOnlyList<UserTaskHistory>> ListRecentByUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default);

    Task<UserTaskHistoryStatsDto> GetStatsByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    void Add(UserTaskHistory history);

    void Update(UserTaskHistory history);
}
