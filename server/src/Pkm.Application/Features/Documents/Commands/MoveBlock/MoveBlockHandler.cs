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
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Documents.Commands.MoveBlock;

public sealed class MoveBlockHandler : ICommandHandler<MoveBlockCommand, BlockMutationDto>
{
    private readonly IDocumentMutationCoordinator _mutationCoordinator;
    private readonly IBlockReadRepository _blockReadRepository;
    private readonly IBlockWriteRepository _blockWriteRepository;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IBlockOrderPlanner _blockOrderPlanner;

    public MoveBlockHandler(
        IDocumentMutationCoordinator mutationCoordinator,
        IBlockReadRepository blockReadRepository,
        IBlockWriteRepository blockWriteRepository,
        IPageWriteRepository pageWriteRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService,
        IBlockOrderPlanner blockOrderPlanner)
    {
        _mutationCoordinator = mutationCoordinator;
        _blockReadRepository = blockReadRepository;
        _blockWriteRepository = blockWriteRepository;
        _pageWriteRepository = pageWriteRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
        _blockOrderPlanner = blockOrderPlanner;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        MoveBlockCommand request,
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

        if (!access.CanReorderBlocks)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockForbidden);

        var lease = await _blockEditLeaseService.GetCurrentAsync(request.BlockId, cancellationToken);
        var leaseError = BlockLeaseGuard.ValidateWriteLease(
            lease,
            currentUserId,
            request.EditorSessionId);

        if (leaseError is not null)
            return Result.Failure<BlockMutationDto>(leaseError);

        var block = await _blockWriteRepository.GetByIdForUpdateAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockNotFound);

        if (request.NewParentBlockId.HasValue)
        {
            var parent = await _blockReadRepository.GetByIdAsync(request.NewParentBlockId.Value, cancellationToken);
            if (parent is null)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockNotFound);

            if (parent.PageId != block.PageId)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);

            var cycle = await _blockReadRepository.IsDescendantOrSelfAsync(
                block.Id,
                request.NewParentBlockId.Value,
                cancellationToken);

            if (cycle)
                return Result.Failure<BlockMutationDto>(DocumentErrors.BlockCycleDetected);
        }

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(block.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        try
        {
            var now = _mutationCoordinator.UtcNow;
            var baseRevision = page.CurrentRevision;
            var oldParentBlockId = block.ParentBlockId;
            var oldOrderKey = block.OrderKey;
            var siblings = await _blockWriteRepository.ListSiblingsForUpdateAsync(
                block.PageId,
                request.NewParentBlockId,
                cancellationToken);

            var orderKeyResult = _blockOrderPlanner.CreateOrderKeyOrRebalance(
                siblings.Where(x => x.Id != block.Id).ToArray(),
                request.PreviousBlockId,
                request.NextBlockId,
                request.NewParentBlockId,
                currentUserId,
                now,
                block.Id);

            if (orderKeyResult.IsFailure)
                return Result.Failure<BlockMutationDto>(orderKeyResult.Error);

            block.MoveTo(request.NewParentBlockId, orderKeyResult.Value, currentUserId, now);

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _mutationCoordinator.AddRevision(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Block moved");

            var payloadJson = JsonSerializer.Serialize(new
            {
                blockId = block.Id,
                newParentBlockId = block.ParentBlockId,
                orderKey = block.OrderKey,
                editorSessionId = request.EditorSessionId
            });

            _mutationCoordinator.AddBlockOperation(
                page.Id,
                block.Id,
                BlockOperationType.Move,
                currentUserId,
                baseRevision,
                appliedRevision,
                now,
                payloadJson,
                "Move block");

            await _mutationCoordinator.SaveChangesAsync(cancellationToken);

            var dto = new BlockMutationDto(
                page.Id,
                block.Id,
                appliedRevision,
                block.ToDto());

            await _mutationCoordinator.RecordActivityAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Move,
                    ActivityEntityType.Block,
                    block.Id,
                    $"{_mutationCoordinator.ActorDisplayName} đã di chuyển block.",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        blockId = block.Id,
                        oldParentBlockId,
                        newParentBlockId = block.ParentBlockId,
                        oldOrderKey,
                        newOrderKey = block.OrderKey,
                        revision = appliedRevision
                    })),
                cancellationToken);

            await _mutationCoordinator.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockMoved",
                    WorkspaceId: access.WorkspaceId,
                    PageId: page.Id,
                    BlockId: block.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Revision: appliedRevision,
                    Payload: dto),
                cancellationToken);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<BlockMutationDto>(new Error(
                "Document.MoveBlockFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}
