using System.Text.Json;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Documents.Services;
using Pkm.Domain.Audit;
using Pkm.Domain.Blocks;

namespace Pkm.Application.Features.Documents.Commands.DeleteBlock;

public sealed class DeleteBlockHandler : ICommandHandler<DeleteBlockCommand, BlockMutationDto>
{
    private readonly IDocumentMutationCoordinator _mutationCoordinator;
    private readonly IBlockWriteRepository _blockWriteRepository;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;

    public DeleteBlockHandler(
        IDocumentMutationCoordinator mutationCoordinator,
        IBlockWriteRepository blockWriteRepository,
        IPageWriteRepository pageWriteRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService)
    {
        _mutationCoordinator = mutationCoordinator;
        _blockWriteRepository = blockWriteRepository;
        _pageWriteRepository = pageWriteRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        DeleteBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BlockId == Guid.Empty)
            return Result.Failure<BlockMutationDto>(DocumentErrors.InvalidBlockId);

        if (!_mutationCoordinator.TryGetCurrentUserId(out var currentUserId))
            return Result.Failure<BlockMutationDto>(DocumentErrors.MissingUserContext);

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockNotFound);

        if (!access.CanDeleteBlocks)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockForbidden);

        var lease = await _blockEditLeaseService.GetCurrentAsync(request.BlockId, cancellationToken);
        var leaseError = BlockLeaseGuard.ValidateWriteLease(
            lease,
            currentUserId,
            request.EditorSessionId);

        if (leaseError is not null)
            return Result.Failure<BlockMutationDto>(leaseError);

        var rootBlock = await _blockWriteRepository.GetByIdForUpdateAsync(request.BlockId, cancellationToken);
        if (rootBlock is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockNotFound);

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(rootBlock.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        var allBlocks = await _blockWriteRepository.ListByPageForUpdateAsync(rootBlock.PageId, cancellationToken);
        var subtree = CollectSubtree(rootBlock.Id, allBlocks);

        var now = _mutationCoordinator.UtcNow;
        var baseRevision = page.CurrentRevision;

        foreach (var block in subtree)
        {
            block.SoftDelete(now);
        }

        var appliedRevision = page.IncreaseRevision(currentUserId, now);

        _mutationCoordinator.AddRevision(
            page.Id,
            appliedRevision,
            currentUserId,
            now,
            "Block subtree deleted");

        var deletedIds = subtree.Select(x => x.Id).ToArray();

        var payloadJson = JsonSerializer.Serialize(new
        {
            rootBlockId = rootBlock.Id,
            deletedBlockIds = deletedIds,
            note = request.Note,
            editorSessionId = request.EditorSessionId
        });

        _mutationCoordinator.AddBlockOperation(
            page.Id,
            rootBlock.Id,
            BlockOperationType.Delete,
            currentUserId,
            baseRevision,
            appliedRevision,
            now,
            payloadJson,
            request.Note ?? "Delete block subtree");

        await _mutationCoordinator.SaveChangesAsync(cancellationToken);

        var dto = new BlockMutationDto(
            page.Id,
            rootBlock.Id,
            appliedRevision,
            null);

        await _mutationCoordinator.RecordActivityAsync(
            new ActivityLogRequest(
                page.WorkspaceId,
                currentUserId,
                ActivityAction.Delete,
                ActivityEntityType.Block,
                rootBlock.Id,
                $"{_mutationCoordinator.ActorDisplayName} đã xóa block.",
                ActivityLogMetadata.Serialize(new
                {
                    pageId = page.Id,
                    rootBlockId = rootBlock.Id,
                    deletedBlockIds = deletedIds,
                    deletedBlockCount = deletedIds.Length,
                    note = request.Note,
                    revision = appliedRevision
                })),
            cancellationToken);

        await _mutationCoordinator.PublishToPageAsync(
            new DocumentRealtimeEnvelope(
                EventName: "BlockDeleted",
                WorkspaceId: access.WorkspaceId,
                PageId: page.Id,
                BlockId: rootBlock.Id,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Revision: appliedRevision,
                Payload: new
                {
                    dto.PageId,
                    dto.BlockId,
                    dto.AppliedRevision,
                    deletedBlockIds = deletedIds
                }),
            cancellationToken);

        return Result.Success(dto);
    }

    private static IReadOnlyList<Block> CollectSubtree(
        Guid rootBlockId,
        IReadOnlyList<Block> allBlocks)
    {
        var childrenByParent = allBlocks
            .Where(x => x.ParentBlockId.HasValue)
            .GroupBy(x => x.ParentBlockId!.Value)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var result = new List<Block>();
        var stack = new Stack<Guid>();
        stack.Push(rootBlockId);

        var byId = allBlocks.ToDictionary(x => x.Id);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!byId.TryGetValue(current, out var block))
                continue;

            result.Add(block);

            if (childrenByParent.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    stack.Push(child.Id);
                }
            }
        }

        return result;
    }
}
