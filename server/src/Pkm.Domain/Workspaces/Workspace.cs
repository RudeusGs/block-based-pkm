using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Workspaces;

/// <summary>
/// Collaborative workspace owned by users.
/// </summary>
public sealed class Workspace : EntityBase
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 500;
    private const int MaxAvatarUrlLength = 2048;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public WorkspaceVisibility Visibility { get; private set; }

    public Guid OwnerId { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    private Workspace() { }

    public Workspace(
        Guid id,
        string name,
        Guid ownerId,
        DateTimeOffset now,
        string? description = null,
        WorkspaceVisibility visibility = WorkspaceVisibility.Private,
        string? avatarUrl = null)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(ownerId, nameof(ownerId));

        OwnerId = ownerId;
        Name = TextRules.NormalizeRequired(name, MaxNameLength, "Workspace name");
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxAvatarUrlLength, nameof(AvatarUrl));
        Visibility = visibility;
    }

    public void UpdateInformation(
        string newName,
        string? newDescription,
        Guid actorId,
        DateTimeOffset now,
        WorkspaceVisibility? visibility = null)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        Name = TextRules.NormalizeRequired(newName, MaxNameLength, "Workspace name");
        Description = TextRules.NormalizeOptional(newDescription, MaxDescriptionLength, nameof(Description));

        if (visibility.HasValue)
            Visibility = visibility.Value;

        LastModifiedBy = actorId;
        Touch(now);
    }

    public void UpdateAvatar(
        string? avatarUrl,
        Guid actorId,
        DateTimeOffset now)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        AvatarUrl = TextRules.NormalizeOptional(avatarUrl, MaxAvatarUrlLength, nameof(AvatarUrl));
        LastModifiedBy = actorId;
        Touch(now);
    }

    public void TransferOwnership(Guid newOwnerId, Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(newOwnerId, nameof(newOwnerId));
        DomainGuard.AgainstEmpty(actorId, nameof(actorId));

        if (OwnerId == newOwnerId)
            return;

        OwnerId = newOwnerId;
        LastModifiedBy = actorId;
        Touch(now);
    }

    public void ChangeVisibility(WorkspaceVisibility visibility, Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, nameof(actorId));
        Visibility = visibility;
        LastModifiedBy = actorId;
        Touch(now);
    }

    public void MoveToTrash(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, nameof(actorId));

        if (IsDeleted)
            return;

        LastModifiedBy = actorId;
        SoftDelete(now);
    }

    public void RestoreFromTrash(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, nameof(actorId));

        if (!IsDeleted)
            return;

        Restore(now);
        LastModifiedBy = actorId;
    }
}
