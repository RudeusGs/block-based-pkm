namespace Pkm.Application.Features.Pages.Policies;

public interface IPageAccessEvaluator
{
    Task<PageAccessResult> EvaluateAsync(
        Guid pageId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record PageAccessResult(
    bool Exists,
    Guid WorkspaceId,
    bool IsArchived,
    bool CanReadPage,
    bool CanCreateSubPage,
    bool CanEditPageMetadata,
    bool CanArchivePage,
    bool CanManagePage,
    bool CanReadDocument,
    bool CanEditDocument,
    bool CanReorderBlocks,
    bool CanDeleteBlocks,
    bool CanAcquireLease)
{
    public bool CanRead => CanReadPage;

    public bool CanWrite =>
        CanEditPageMetadata ||
        CanCreateSubPage ||
        CanEditDocument ||
        CanArchivePage;
}