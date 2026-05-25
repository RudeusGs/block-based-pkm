using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Workspaces;

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
        EnsureValidRole(role);

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
        EnsureValidRole(newRole);

        if (newRole == WorkspaceRole.Owner)
            throw new DomainException("Owner role must be assigned through workspace ownership transfer.");

        if (IsOwner())
            throw new DomainException("Owner membership cannot be changed through regular role management.");

        if (Role == newRole) return;

        Role = newRole;
        Touch(now);
    }

    public void PromoteToOwner(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Role == WorkspaceRole.Owner)
            return;

        Role = WorkspaceRole.Owner;
        Touch(now);
    }

    public void DemoteOwnerToManager(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Role != WorkspaceRole.Owner)
            return;

        Role = WorkspaceRole.Manager;
        Touch(now);
    }

    public bool IsOwner() => Role == WorkspaceRole.Owner;
    public bool IsManager() => Role == WorkspaceRole.Manager;

    private static void EnsureValidRole(WorkspaceRole role)
    {
        if (!Enum.IsDefined(typeof(WorkspaceRole), role))
            throw new DomainException("Workspace role is invalid.");
    }
}
