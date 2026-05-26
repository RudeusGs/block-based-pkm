using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Pages;

/// <summary>
/// Represents a workspace page, including metadata, hierarchy, revision state, and archive state.
/// </summary>
public sealed class Page : EntityBase
{
    private const int MaxTitleLength = 200;
    private const int MaxIconLength = 500;
    private const int MaxCoverImageLength = 2048;
    private const int MaxPublicTokenLength = 128;

    public Guid WorkspaceId { get; private set; }
    public Guid? ParentPageId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? CoverImage { get; private set; }

    public Guid CreatedBy { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsPublished { get; private set; }
    public string? PublicToken { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }

    public long CurrentRevision { get; private set; }

    private Page() { }

    public Page(
        Guid id,
        Guid workspaceId,
        string title,
        Guid createdBy,
        DateTimeOffset now,
        Guid? parentPageId = null,
        string? icon = null,
        string? coverImage = null) : base(id, now)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(createdBy, nameof(createdBy));

        WorkspaceId = workspaceId;
        ParentPageId = parentPageId;
        CreatedBy = createdBy;
        IsArchived = false;
        IsPublished = false;
        CurrentRevision = 0;

        Title = TextRules.NormalizeOrDefault(title, "Untitled", MaxTitleLength, nameof(Title));
        Icon = TextRules.NormalizeOptional(icon, MaxIconLength, nameof(Icon));
        CoverImage = TextRules.NormalizeOptional(coverImage, MaxCoverImageLength, nameof(CoverImage));
    }

    public void Rename(string newTitle, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);

        Title = TextRules.NormalizeOrDefault(newTitle, "Untitled", MaxTitleLength, nameof(Title));
        RegisterModification(actorId, now);
    }

    public void UpdateAppearance(string? icon, string? coverImage, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);

        Icon = TextRules.NormalizeOptional(icon, MaxIconLength, nameof(Icon));
        CoverImage = TextRules.NormalizeOptional(coverImage, MaxCoverImageLength, nameof(CoverImage));

        RegisterModification(actorId, now);
    }

    public void MoveTo(Guid targetWorkspaceId, Guid? targetParentPageId, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);
        DomainGuard.AgainstEmpty(targetWorkspaceId, "TargetWorkspaceId");

        if (targetParentPageId.HasValue && targetParentPageId == Id)
            throw new DomainException("A page cannot be moved into itself.");

        if (targetParentPageId.HasValue && targetWorkspaceId != WorkspaceId)
            throw new DomainException("A nested page cannot be moved to another workspace while it still has a parent.");

        WorkspaceId = targetWorkspaceId;
        ParentPageId = targetParentPageId;

        RegisterModification(actorId, now);
    }

    public void Archive(Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureValidActor(actorId);

        if (IsArchived) return;

        IsArchived = true;
        ArchivedAt = now;

        RegisterModification(actorId, now);
    }

    public void RestoreFromArchive(Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureValidActor(actorId);

        if (!IsArchived) return;

        IsArchived = false;
        ArchivedAt = null;

        RegisterModification(actorId, now);
    }


    public void Publish(string publicToken, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);

        var normalizedToken = TextRules.NormalizeRequired(
            publicToken,
            MaxPublicTokenLength,
            nameof(PublicToken));

        IsPublished = true;
        PublicToken = normalizedToken;
        PublishedAt = now;
        PublishedBy = actorId;

        RegisterModification(actorId, now);
    }

    public void Unpublish(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);

        if (!IsPublished && PublicToken is null)
            return;

        IsPublished = false;
        PublicToken = null;
        PublishedAt = null;
        PublishedBy = null;

        RegisterModification(actorId, now);
    }

    public long IncreaseRevision(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable();
        EnsureValidActor(actorId);

        CurrentRevision++;
        RegisterModification(actorId, now);

        return CurrentRevision;
    }

    private void EnsureEditable()
    {
        ThrowIfDeleted();

        if (IsArchived)
            throw new DomainException("Archived pages cannot be edited.");
    }

    private void RegisterModification(Guid actorId, DateTimeOffset now)
    {
        LastModifiedBy = actorId;
        Touch(now);
    }

    private static void EnsureValidActor(Guid actorId)
        => DomainGuard.AgainstEmpty(actorId, "ActorId");
}



