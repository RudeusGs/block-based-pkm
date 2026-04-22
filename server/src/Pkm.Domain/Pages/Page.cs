using Pkm.Domain.Common;

namespace Pkm.Domain.Pages;

/// <summary>
/// Page: Đại diện cho một trang trong hệ thống, chứa thông tin về tiêu đề, biểu tượng, hình bìa, và trạng thái lưu trữ.
/// </summary>
public sealed class Page : EntityBase
{
    private const int MaxTitleLength = 200;
    private const int MaxIconLength = 500;
    private const int MaxCoverImageLength = 2048;

    public Guid WorkspaceId { get; private set; }
    public Guid? ParentPageId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? CoverImage { get; private set; }

    public Guid CreatedBy { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

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
            throw new DomainException("Không thể chuyển page vào chính nó.");

        if (targetParentPageId.HasValue && targetWorkspaceId != WorkspaceId)
            throw new DomainException("Không thể chuyển page sang workspace khác khi vẫn có parent.");

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
            throw new DomainException("Không thể chỉnh sửa page đã archive.");
    }

    private void RegisterModification(Guid actorId, DateTimeOffset now)
    {
        LastModifiedBy = actorId;
        Touch(now);
    }

    private static void EnsureValidActor(Guid actorId)
        => DomainGuard.AgainstEmpty(actorId, "ActorId");
}