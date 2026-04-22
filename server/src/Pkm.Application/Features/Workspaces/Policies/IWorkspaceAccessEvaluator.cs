using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Policies;

public interface IWorkspaceAccessEvaluator
{
    Task<WorkspaceAccessResult> EvaluateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkspaceAccessResult(
    bool Exists,
    Guid WorkspaceId,
    WorkspaceRole? Role,
    bool CanReadWorkspace,
    bool CanUpdateWorkspace,
    bool CanDeleteWorkspace,
    bool CanManageMembers,
    bool CanManageWorkspaceSettings,
    bool CanCreatePages,
    bool CanCreateTasks,
    bool CanReadAudit,
    bool CanManageAuditRetention)
{
    public bool CanRead => CanReadWorkspace;

    public bool CanWrite =>
        CanUpdateWorkspace ||
        CanCreatePages ||
        CanCreateTasks;
}