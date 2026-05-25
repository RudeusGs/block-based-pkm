using Pkm.Domain.Recommendations;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IUserTaskPreferenceRepository
{
    Task<UserTaskPreference?> GetByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<UserTaskPreference?> GetByUserAndWorkspaceForUpdateAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    void Add(UserTaskPreference preference);

    void Update(UserTaskPreference preference);
}
