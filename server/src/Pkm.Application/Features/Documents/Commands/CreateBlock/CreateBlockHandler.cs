using System.Text.Json;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Blocks;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Documents.Commands.CreateBlock;

public sealed class CreateBlockHandler : ICommandHandler<CreateBlockCommand, BlockMutationDto>
{
    private readonly IDocumentMutationCoordinator _mutationCoordinator;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IBlockReadRepository _blockReadRepository;
    private readonly IBlockWriteRepository _blockWriteRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IBlockOrderPlanner _blockOrderPlanner;
    private readonly IBlockPayloadValidator _blockPayloadValidator;

    public CreateBlockHandler(
        IDocumentMutationCoordinator mutationCoordinator,
        IPageWriteRepository pageWriteRepository,
        IBlockReadRepository blockReadRepository,
        IBlockWriteRepository blockWriteRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IBlockOrderPlanner blockOrderPlanner,
        IBlockPayloadValidator blockPayloadValidator)
    {
        _mutationCoordinator = mutationCoordinator;
        _pageWriteRepository = pageWriteRepository;
        _blockReadRepository = blockReadRepository;
        _blockWriteRepository = blockWriteRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _blockOrderPlanner = blockOrderPlanner;
        _blockPayloadValidator = blockPayloadValidator;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        CreateBlockCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<BlockMutationDto>(DocumentErrors.InvalidPageId);

        if (!_mutationCoordinator.TryGetCurrentUserId(out var currentUserId))
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
            var now = _mutationCoordinator.UtcNow;
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

            _mutationCoordinator.AddRevision(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Block created");

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

            _mutationCoordinator.AddBlockOperation(
                page.Id,
                block.Id,
                BlockOperationType.Create,
                currentUserId,
                baseRevision,
                appliedRevision,
                now,
                payloadJson,
                "Create block");

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
                    ActivityAction.Create,
                    ActivityEntityType.Block,
                    block.Id,
                    $"{_mutationCoordinator.ActorDisplayName} đã tạo block {block.Type.Value}.",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        blockId = block.Id,
                        type = block.Type.Value,
                        parentBlockId = block.ParentBlockId,
                        revision = appliedRevision
                    })),
                cancellationToken);

            await _mutationCoordinator.PublishToPageAsync(
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
