using System.Text.Json;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Blocks;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Documents.Commands.CreateBlock;

public sealed class CreateBlockHandler : ICommandHandler<CreateBlockCommand, BlockMutationDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IBlockReadRepository _blockReadRepository;
    private readonly IBlockWriteRepository _blockWriteRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IBlockOperationRepository _blockOperationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IBlockOrderPlanner _blockOrderPlanner;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IBlockPayloadValidator _blockPayloadValidator;
    private readonly IActivityLogService _activityLogService;

    public CreateBlockHandler(
        ICurrentUser currentUser,
        IPageWriteRepository pageWriteRepository,
        IBlockReadRepository blockReadRepository,
        IBlockWriteRepository blockWriteRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageRevisionRepository pageRevisionRepository,
        IBlockOperationRepository blockOperationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IBlockOrderPlanner blockOrderPlanner,
        IDocumentRealtimePublisher realtimePublisher,
        IBlockPayloadValidator blockPayloadValidator,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageWriteRepository = pageWriteRepository;
        _blockReadRepository = blockReadRepository;
        _blockWriteRepository = blockWriteRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRevisionRepository = pageRevisionRepository;
        _blockOperationRepository = blockOperationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _blockOrderPlanner = blockOrderPlanner;
        _realtimePublisher = realtimePublisher;
        _blockPayloadValidator = blockPayloadValidator;
        _activityLogService = activityLogService;
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

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        if (page.IsArchived)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageArchived);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        if (request.ParentBlockId.HasValue)
        {
            var parentBlock = await _blockReadRepository.GetByIdAsync(request.ParentBlockId.Value, cancellationToken);

            if (parentBlock is null)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockNotFound);

            if (parentBlock.PageId != request.PageId)
                return Result.Failure<BlockMutationDto>(DocumentErrors.ParentBlockDifferentPage);
        }

        var siblings = await _blockWriteRepository.ListSiblingsForUpdateAsync(
            request.PageId,
            request.ParentBlockId,
            cancellationToken);

        var propsError = _blockPayloadValidator.ValidatePropsJson(request.PropsJson);
        if (propsError is not null)
            return Result.Failure<BlockMutationDto>(propsError);

        try
        {
            var now = _clock.UtcNow;
            var baseRevision = page.CurrentRevision;
            var blockId = Guid.NewGuid();

            var orderKeyResult = _blockOrderPlanner.CreateOrderKeyOrRebalance(
                siblings,
                request.PreviousBlockId,
                request.NextBlockId,
                request.ParentBlockId,
                currentUserId,
                now,
                blockId);

            if (orderKeyResult.IsFailure)
                return Result.Failure<BlockMutationDto>(orderKeyResult.Error);

            var block = new Block(
                blockId,
                request.PageId,
                BlockTypeCode.From(request.Type),
                orderKeyResult.Value,
                currentUserId,
                now,
                request.TextContent,
                request.PropsJson,
                request.ParentBlockId,
                request.SchemaVersion);

            _blockWriteRepository.Add(block);

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

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.Block,
                    block.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã tạo block {block.Type.Value}.",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        blockId = block.Id,
                        type = block.Type.Value,
                        parentBlockId = block.ParentBlockId,
                        revision = appliedRevision
                    })),
                cancellationToken);

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
