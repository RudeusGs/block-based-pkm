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

        Pkm.Domain.Blocks.Block? parentBlock = null;
        if (request.ParentBlockId.HasValue)
        {
            parentBlock = await _blockRepository.GetByIdAsync(request.ParentBlockId.Value, cancellationToken);

            if (parentBlock is null)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockNotFound);

            if (parentBlock.PageId != request.PageId)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        var previous = request.PreviousBlockId.HasValue
            ? await _blockRepository.GetByIdAsync(request.PreviousBlockId.Value, cancellationToken)
            : null;

        var next = request.NextBlockId.HasValue
            ? await _blockRepository.GetByIdAsync(request.NextBlockId.Value, cancellationToken)
            : null;

        if (previous is not null &&
            (previous.PageId != request.PageId || previous.ParentBlockId != request.ParentBlockId))
        {
            return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        if (next is not null &&
            (next.PageId != request.PageId || next.ParentBlockId != request.ParentBlockId))
        {
            return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }
        var propsError = _blockPayloadValidator.ValidatePropsJson(request.PropsJson);
        if (propsError is not null)
            return Result.Failure<BlockMutationDto>(propsError);
        try
        {
            var now = _clock.UtcNow;
            var baseRevision = page.CurrentRevision;
            var orderKey = _orderKeyGenerator.CreateBetween(previous?.OrderKey, next?.OrderKey);

            var block = new Block(
                Guid.NewGuid(),
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
}