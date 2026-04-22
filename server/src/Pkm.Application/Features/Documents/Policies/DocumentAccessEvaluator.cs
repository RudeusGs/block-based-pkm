using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Authorization;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Documents.Policies;

public sealed class DocumentAccessEvaluator : IDocumentAccessEvaluator
{
    private readonly IBlockRepository _blockRepository;

    public DocumentAccessEvaluator(IBlockRepository blockRepository)
    {
        _blockRepository = blockRepository;
    }

    public async Task<DocumentBlockAccessResult> EvaluateByBlockAsync(
        Guid blockId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty || userId == Guid.Empty)
        {
            return new DocumentBlockAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                PageId: Guid.Empty,
                IsPageArchived: false,
                CanReadDocument: false,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false,
                CanManagePage: false);
        }

        var access = await _blockRepository.GetAccessContextAsync(
            blockId,
            userId,
            cancellationToken);

        if (access is null)
        {
            return new DocumentBlockAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                PageId: Guid.Empty,
                IsPageArchived: false,
                CanReadDocument: false,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false,
                CanManagePage: false);
        }

        var isOwner = access.OwnerId == userId || access.Role == WorkspaceRole.Owner;
        var capabilities = WorkspaceRoleCapabilityMatrix.ForDocument(
            isOwner,
            access.Role,
            access.IsPageArchived);

        return new DocumentBlockAccessResult(
            Exists: true,
            WorkspaceId: access.WorkspaceId,
            PageId: access.PageId,
            IsPageArchived: access.IsPageArchived,
            CanReadDocument: capabilities.CanReadDocument,
            CanEditDocument: capabilities.CanEditDocument,
            CanReorderBlocks: capabilities.CanReorderBlocks,
            CanDeleteBlocks: capabilities.CanDeleteBlocks,
            CanAcquireLease: capabilities.CanAcquireLease,
            CanManagePage: capabilities.CanManagePage);
    }
}