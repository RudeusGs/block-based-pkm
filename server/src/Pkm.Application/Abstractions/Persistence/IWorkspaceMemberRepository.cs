using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkspaceMemberRepository
{
    Task<WorkspaceMember?> GetByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceMember>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceMemberReadModel>> ListReadModelsByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(WorkspaceMember member);
    void Update(WorkspaceMember member);
}