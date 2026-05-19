using Pkm.Domain.Workspaces;

namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkspaceInvitationRepository
{
    Task<WorkspaceInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(
        Guid workspaceId,
        string normalizedEmail,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    void Add(WorkspaceInvitation invitation);

    void Update(WorkspaceInvitation invitation);
}
