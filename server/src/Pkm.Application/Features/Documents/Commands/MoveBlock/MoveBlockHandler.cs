using System.Text.Json;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Documents.Services;
using Pkm.Domain.Blocks;
using Pkm.Domain.Common;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Documents.Commands.MoveBlock;

public sealed class MoveBlockHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IBlockRepository _blockRepository;
    private readonly IPageRepository _pageRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IBlockOperationRepository _blockOperationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrderKeyGenerator _orderKeyGenerator;
    private readonly IDocumentRealtimePublisher _realtimePublisher;

    public MoveBlockHandler(
        ICurrentUser currentUser,
        IBlockRepository blockRepository,
        IPageRepository pageRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService,
        IPageRevisionRepository pageRevisionRepository,
        IBlockOperationRepository blockOperationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IOrderKeyGenerator orderKeyGenerator,
        IDocumentRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _blockRepository = blockRepository;
        _pageRepository = pageRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
        _pageRevisionRepository = pageRevisionRepository;
        _blockOperationRepository = blockOperationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _orderKeyGenerator = orderKeyGenerator;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        MoveBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BlockId == Guid.Empty)
            return Result.Failure<BlockMutationDto>(DocumentErrors.InvalidBlockId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
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

        var block = await _blockRepository.GetByIdForUpdateAsync(request.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.BlockNotFound);

        if (request.NewParentBlockId.HasValue)
        {
            var parent = await _blockRepository.GetByIdAsync(request.NewParentBlockId.Value, cancellationToken);
            if (parent is null)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockNotFound);

            if (parent.PageId != block.PageId)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);

            var cycle = await _blockRepository.IsDescendantOrSelfAsync(
                block.Id,
                request.NewParentBlockId.Value,
                cancellationToken);

            if (cycle)
                return Result.Failure<BlockMutationDto>(DocumentErrors.BlockCycleDetected);
        }

        var page = await _pageRepository.GetByIdForUpdateAsync(block.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        var previous = request.PreviousBlockId.HasValue
            ? await _blockRepository.GetByIdAsync(request.PreviousBlockId.Value, cancellationToken)
            : null;

        var next = request.NextBlockId.HasValue
            ? await _blockRepository.GetByIdAsync(request.NextBlockId.Value, cancellationToken)
            : null;

        if (previous is not null &&
            (previous.PageId != block.PageId || previous.ParentBlockId != request.NewParentBlockId))
        {
            return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        if (next is not null &&
            (next.PageId != block.PageId || next.ParentBlockId != request.NewParentBlockId))
        {
            return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        try
        {
            var now = _clock.UtcNow;
            var baseRevision = page.CurrentRevision;
            var newOrderKey = _orderKeyGenerator.CreateBetween(previous?.OrderKey, next?.OrderKey);

            block.MoveTo(request.NewParentBlockId, newOrderKey, currentUserId, now);

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _pageRevisionRepository.Add(PageRevision.Create(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Block moved"));

            var payloadJson = JsonSerializer.Serialize(new
            {
                blockId = block.Id,
                newParentBlockId = block.ParentBlockId,
                orderKey = block.OrderKey,
                editorSessionId = request.EditorSessionId
            });

            _blockOperationRepository.Add(BlockOperation.Create(
                page.Id,
                block.Id,
                BlockOperationType.Move,
                currentUserId,
                baseRevision,
                appliedRevision,
                now,
                payloadJson,
                "Move block"));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new BlockMutationDto(
                page.Id,
                block.Id,
                appliedRevision,
                block.ToDto());

            await _realtimePublisher.PublishToPageAsync(
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