using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceInvitationRepository : IWorkspaceInvitationRepository
{
    private readonly DataContext _dataContext;

    public WorkspaceInvitationRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<WorkspaceInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dataContext.WorkspaceInvitations
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    public Task<WorkspaceInvitation?> GetPendingByWorkspaceAndEmailAsync(
        Guid workspaceId,
        string normalizedEmail,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        return _dataContext.WorkspaceInvitations
            .AsNoTracking()
            .Where(x =>
                x.WorkspaceId == workspaceId &&
                x.NormalizedEmail == normalizedEmail &&
                x.AcceptedAtUtc == null &&
                x.ExpiresAtUtc > utcNow)
            .OrderByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(WorkspaceInvitation invitation)
    {
        _dataContext.WorkspaceInvitations.Add(invitation);
    }

    public void Update(WorkspaceInvitation invitation)
    {
        _dataContext.WorkspaceInvitations.Update(invitation);
    }
}
