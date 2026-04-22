using Pkm.Domain.Common;

namespace Pkm.Domain.Workspaces;

/// <summary>
/// WorkspaceMember: đại diện cho một thành viên trong workspace và vai trò hiện tại của họ.
/// </summary>
public sealed class WorkspaceMember : EntityBase
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }

    private WorkspaceMember() { }

    private WorkspaceMember(
        Guid id,
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        DateTimeOffset now) : base(id, now)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
    }

    public static WorkspaceMember CreateOwner(Guid workspaceId, Guid ownerId, DateTimeOffset now)
        => new(Guid.NewGuid(), workspaceId, ownerId, WorkspaceRole.Owner, now);

    public static WorkspaceMember CreateMember(Guid workspaceId, Guid userId, DateTimeOffset now)
        => new(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Member, now);

    public static WorkspaceMember CreateManager(Guid workspaceId, Guid userId, DateTimeOffset now)
        => new(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Manager, now);
    public static WorkspaceMember CreateViewer(Guid workspaceId, Guid userId, DateTimeOffset now)
    => new(Guid.NewGuid(), workspaceId, userId, WorkspaceRole.Viewer, now);
    public void ChangeRole(WorkspaceRole newRole, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Role == newRole) return;

        Role = newRole;
        Touch(now);
    }

    public bool IsOwner() => Role == WorkspaceRole.Owner;
    public bool IsManager() => Role == WorkspaceRole.Manager;
}