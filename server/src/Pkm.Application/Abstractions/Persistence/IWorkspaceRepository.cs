using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkspaceRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceMember?> GetMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsOwnerAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);

    Task<WorkspaceAccessReadModel?> GetAccessContextAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceDetailReadModel?> GetDetailAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceListItemReadModel>> ListByUserAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Workspace workspace);
    void Update(Workspace workspace);
}
