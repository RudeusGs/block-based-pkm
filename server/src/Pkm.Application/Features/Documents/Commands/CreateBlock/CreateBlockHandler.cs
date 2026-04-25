using System.Text.Json;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Blocks;
using Pkm.Domain.Common;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Documents.Commands.CreateBlock;

public sealed class CreateBlockHandler
{
    private const int RebalanceOrderKeyLengthThreshold = 90;

    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IBlockOperationRepository _blockOperationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IOrderKeyGenerator _orderKeyGenerator;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IBlockPayloadValidator _blockPayloadValidator;

    public CreateBlockHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IBlockRepository blockRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageRevisionRepository pageRevisionRepository,
        IBlockOperationRepository blockOperationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IOrderKeyGenerator orderKeyGenerator,
        IDocumentRealtimePublisher realtimePublisher,
        IBlockPayloadValidator blockPayloadValidator)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _blockRepository = blockRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRevisionRepository = pageRevisionRepository;
        _blockOperationRepository = blockOperationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _orderKeyGenerator = orderKeyGenerator;
        _realtimePublisher = realtimePublisher;
        _blockPayloadValidator = blockPayloadValidator;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        CreateBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<BlockMutationDto>(DocumentErrors.InvalidPageId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<BlockMutationDto>(DocumentErrors.MissingUserContext);

        var pageAccess = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!pageAccess.Exists)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        if (!pageAccess.CanWrite)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageForbidden);

        var page = await _pageRepository.GetByIdForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        if (page.IsArchived)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageArchived);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        if (request.ParentBlockId.HasValue)
        {
            var parentBlock = await _blockRepository.GetByIdAsync(request.ParentBlockId.Value, cancellationToken);

            if (parentBlock is null)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockNotFound);

            if (parentBlock.PageId != request.PageId)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        var siblings = await _blockRepository.ListSiblingsForUpdateAsync(
            request.PageId,
            request.ParentBlockId,
            cancellationToken);

        var position = ResolveInsertionPosition(
            siblings,
            request.PreviousBlockId,
            request.NextBlockId);

        if (position.Error is not null)
            return Result.Failure<BlockMutationDto>(position.Error);

        var propsError = _blockPayloadValidator.ValidatePropsJson(request.PropsJson);
        if (propsError is not null)
            return Result.Failure<BlockMutationDto>(propsError);

        try
        {
            var now = _clock.UtcNow;
            var baseRevision = page.CurrentRevision;
            var blockId = Guid.NewGuid();

            var orderKey = CreateOrderKeyOrRebalance(
                siblings,
                position,
                request.ParentBlockId,
                currentUserId,
                now,
                blockId);

            var block = new Block(
                blockId,
                request.PageId,
                BlockTypeCode.From(request.Type),
                orderKey,
                currentUserId,
                now,
                request.TextContent,
                request.PropsJson,
                request.ParentBlockId,
                request.SchemaVersion);

            _blockRepository.Add(block);

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _pageRevisionRepository.Add(PageRevision.Create(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Block created"));

            var payloadJson = JsonSerializer.Serialize(new
            {
                blockId = block.Id,
                parentBlockId = block.ParentBlockId,
                type = block.Type.Value,
                textContent = block.TextContent,
                propsJson = block.PropsJson,
                schemaVersion = block.SchemaVersion,
                orderKey = block.OrderKey
            });

            _blockOperationRepository.Add(BlockOperation.Create(
                page.Id,
                block.Id,
                BlockOperationType.Create,
                currentUserId,
                baseRevision,
                appliedRevision,
                now,
                payloadJson,
                "Create block"));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new BlockMutationDto(
                page.Id,
                block.Id,
                appliedRevision,
                block.ToDto());

            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockCreated",
                    WorkspaceId: pageAccess.WorkspaceId,
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
                "Document.CreateBlockFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private string CreateOrderKeyOrRebalance(
        IReadOnlyList<Block> siblings,
        InsertionPosition position,
        Guid? parentBlockId,
        Guid actorId,
        DateTimeOffset now,
        Guid newBlockId)
    {
        try
        {
            var candidate = _orderKeyGenerator.CreateBetween(
                position.Previous?.OrderKey,
                position.Next?.OrderKey);

            if (!NeedsRebalance(candidate, siblings))
                return candidate;
        }
        catch (DomainException)
        {

        }

        RebalanceSiblings(
            siblings,
            position.InsertIndex,
            parentBlockId,
            actorId,
            now);

        return CreateRebalancedOrderKey(position.InsertIndex, newBlockId);
    }

    private static bool NeedsRebalance(
        string candidate,
        IReadOnlyList<Block> siblings)
    {
        return string.IsNullOrWhiteSpace(candidate) ||
               candidate.Length > RebalanceOrderKeyLengthThreshold ||
               siblings.Any(x => string.Equals(x.OrderKey, candidate, StringComparison.Ordinal));
    }

    private static void RebalanceSiblings(
        IReadOnlyList<Block> siblings,
        int insertIndex,
        Guid? parentBlockId,
        Guid actorId,
        DateTimeOffset now)
    {
        for (var index = 0; index < siblings.Count; index++)
        {
            var finalIndex = index < insertIndex
                ? index
                : index + 1;

            var newOrderKey = CreateRebalancedOrderKey(finalIndex, siblings[index].Id);

            if (string.Equals(siblings[index].OrderKey, newOrderKey, StringComparison.Ordinal))
                continue;

            siblings[index].MoveTo(parentBlockId, newOrderKey, actorId, now);
        }
    }

    private static string CreateRebalancedOrderKey(int index, Guid stableId)
        => $"M{index + 1:D12}{stableId:N}";

    private static InsertionPosition ResolveInsertionPosition(
        IReadOnlyList<Block> siblings,
        Guid? previousBlockId,
        Guid? nextBlockId)
    {
        var ordered = siblings
            .OrderBy(x => x.OrderKey, StringComparer.Ordinal)
            .ToArray();

        if (previousBlockId.HasValue &&
            nextBlockId.HasValue &&
            previousBlockId.Value == nextBlockId.Value)
        {
            return InsertionPosition.Failure(DocumentErrors.InvalidBlockPosition);
        }

        var previousIndex = -1;
        var nextIndex = -1;
        Block? previous = null;
        Block? next = null;

        if (previousBlockId.HasValue)
        {
            previousIndex = Array.FindIndex(ordered, x => x.Id == previousBlockId.Value);

            if (previousIndex < 0)
                return InsertionPosition.Failure(DocumentErrors.BlockNotFound);

            previous = ordered[previousIndex];
        }

        if (nextBlockId.HasValue)
        {
            nextIndex = Array.FindIndex(ordered, x => x.Id == nextBlockId.Value);

            if (nextIndex < 0)
                return InsertionPosition.Failure(DocumentErrors.BlockNotFound);

            next = ordered[nextIndex];
        }

        if (previous is not null && next is not null)
        {
            if (previousIndex + 1 != nextIndex)
                return InsertionPosition.Failure(DocumentErrors.InvalidBlockPosition);

            return new InsertionPosition(
                previous,
                next,
                nextIndex,
                Error: null);
        }

        if (previous is not null)
        {
            var insertIndex = previousIndex + 1;
            next = insertIndex < ordered.Length ? ordered[insertIndex] : null;

            return new InsertionPosition(
                previous,
                next,
                insertIndex,
                Error: null);
        }

        if (next is not null)
        {
            var insertIndex = nextIndex;
            previous = insertIndex > 0 ? ordered[insertIndex - 1] : null;

            return new InsertionPosition(
                previous,
                next,
                insertIndex,
                Error: null);
        }

        return new InsertionPosition(
            ordered.LastOrDefault(),
            Next: null,
            InsertIndex: ordered.Length,
            Error: null);
    }

    private sealed record InsertionPosition(
        Block? Previous,
        Block? Next,
        int InsertIndex,
        Error? Error)
    {
        public static InsertionPosition Failure(Error error)
            => new(
                Previous: null,
                Next: null,
                InsertIndex: 0,
                Error: error);
    }
}