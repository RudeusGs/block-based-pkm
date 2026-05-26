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

namespace Pkm.Application.Features.Documents.Commands.UpdateBlock;

public sealed class UpdateBlockHandler : ICommandHandler<UpdateBlockCommand, BlockMutationDto>
{
    private readonly IDocumentMutationCoordinator _mutationCoordinator;
    private readonly IBlockWriteRepository _blockWriteRepository;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IBlockPayloadValidator _blockPayloadValidator;

    public UpdateBlockHandler(
        IDocumentMutationCoordinator mutationCoordinator,
        IBlockWriteRepository blockWriteRepository,
        IPageWriteRepository pageWriteRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService,
        IBlockPayloadValidator blockPayloadValidator)
    {
        _mutationCoordinator = mutationCoordinator;
        _blockWriteRepository = blockWriteRepository;
        _pageWriteRepository = pageWriteRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
        _blockPayloadValidator = blockPayloadValidator;
    }

    public async Task<Result<BlockMutationDto>> HandleAsync(
        UpdateBlockCommand request,
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

        if (!access.CanEditDocument)
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

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(block.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<BlockMutationDto>(DocumentErrors.PageNotFound);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(page, request.ExpectedRevision);
        if (revisionError is not null)
            return Result.Failure<BlockMutationDto>(revisionError);

        var propsError = _blockPayloadValidator.ValidatePropsJson(request.PropsJson);
        if (propsError is not null)
            return Result.Failure<BlockMutationDto>(propsError);

        var oldType = block.Type.Value;
        var oldText = block.TextContent;
        var oldProps = block.PropsJson;

        try
        {
            var now = _mutationCoordinator.UtcNow;
            var baseRevision = page.CurrentRevision;

            if (!string.IsNullOrWhiteSpace(request.Type) && request.Type != block.Type.Value)
            {
                block.ChangeType(BlockTypeCode.From(request.Type), currentUserId, now);
            }

            block.UpdateContent(request.TextContent, request.PropsJson, currentUserId, now);

            var hasTypeChanged = oldType != block.Type.Value;
            var hasTextChanged = oldText != block.TextContent;
            var hasPropsChanged = oldProps != block.PropsJson;

            if (!hasTypeChanged && !hasTextChanged && !hasPropsChanged)
            {
                return Result.Success(new BlockMutationDto(
                    page.Id,
                    block.Id,
                    page.CurrentRevision,
                    block.ToDto()));
            }

            var operationType = hasTypeChanged
                ? BlockOperationType.ChangeType
                : hasTextChanged && hasPropsChanged
                    ? BlockOperationType.UpdateContent
                    : hasTextChanged
                        ? BlockOperationType.UpdateText
                        : BlockOperationType.UpdateProps;

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _mutationCoordinator.AddRevision(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Block updated");

            var payloadJson = JsonSerializer.Serialize(new
            {
                blockId = block.Id,
                oldType,
                newType = block.Type.Value,
                oldText,
                newText = block.TextContent,
                oldProps,
                newProps = block.PropsJson,
                editorSessionId = request.EditorSessionId
            });

            _mutationCoordinator.AddBlockOperation(
                page.Id,
                block.Id,
                operationType,
                currentUserId,
                baseRevision,
                appliedRevision,
                now,
                payloadJson,
                "Update block");

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
                    ActivityAction.Update,
                    ActivityEntityType.Block,
                    block.Id,
                    $"{_mutationCoordinator.ActorDisplayName} đã cập nhật block {block.Type.Value}.",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        blockId = block.Id,
                        operationType = operationType.ToString(),
                        oldType,
                        newType = block.Type.Value,
                        oldTextPreview = Preview(oldText),
                        newTextPreview = Preview(block.TextContent),
                        revision = appliedRevision
                    })),
                cancellationToken);

            await _mutationCoordinator.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockUpdated",
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
                "Document.UpdateBlockFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private static string? Preview(string? value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return normalized.Length <= 160
            ? normalized
            : $"{normalized[..160]}...";
    }
}
