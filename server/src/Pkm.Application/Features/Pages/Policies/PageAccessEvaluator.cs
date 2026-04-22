using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Authorization;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Pages.Policies;

public sealed class PageAccessEvaluator : IPageAccessEvaluator
{
    private readonly IPageRepository _pageRepository;

    public PageAccessEvaluator(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<PageAccessResult> EvaluateAsync(
        Guid pageId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty || userId == Guid.Empty)
        {
            return new PageAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                IsArchived: false,
                CanReadPage: false,
                CanCreateSubPage: false,
                CanEditPageMetadata: false,
                CanArchivePage: false,
                CanManagePage: false,
                CanReadDocument: false,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false);
        }

        var access = await _pageRepository.GetAccessContextAsync(
            pageId,
            userId,
            cancellationToken);

        if (access is null)
        {
            return new PageAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                IsArchived: false,
                CanReadPage: false,
                CanCreateSubPage: false,
                CanEditPageMetadata: false,
                CanArchivePage: false,
                CanManagePage: false,
                CanReadDocument: false,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false);
        }

        var isOwner = access.OwnerId == userId || access.Role == WorkspaceRole.Owner;
        var capabilities = WorkspaceRoleCapabilityMatrix.ForPage(
            isOwner,
            access.Role,
            access.IsArchived);

        return new PageAccessResult(
            Exists: true,
            WorkspaceId: access.WorkspaceId,
            IsArchived: access.IsArchived,
            CanReadPage: capabilities.CanReadPage,
            CanCreateSubPage: capabilities.CanCreateSubPage,
            CanEditPageMetadata: capabilities.CanEditPageMetadata,
            CanArchivePage: capabilities.CanArchivePage,
            CanManagePage: capabilities.CanManagePage,
            CanReadDocument: capabilities.CanReadDocument,
            CanEditDocument: capabilities.CanEditDocument,
            CanReorderBlocks: capabilities.CanReorderBlocks,
            CanDeleteBlocks: capabilities.CanDeleteBlocks,
            CanAcquireLease: capabilities.CanAcquireLease);
    }
}