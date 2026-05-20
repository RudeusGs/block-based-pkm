using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkspaceMemberRepository
{
    Task<WorkspaceMember?> GetByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceMemberReadModel?> GetDetailByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceMemberReadModel>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(WorkspaceMember member);

    void Update(WorkspaceMember member);

    void Remove(WorkspaceMember member);
}