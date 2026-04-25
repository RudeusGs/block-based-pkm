using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Blocks;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Blocks;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Commands.AcquireBlockLease;
using Pkm.Application.Features.Documents.Commands.CreateBlock;
using Pkm.Application.Features.Documents.Commands.DeleteBlock;
using Pkm.Application.Features.Documents.Commands.MoveBlock;
using Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;
using Pkm.Application.Features.Documents.Commands.RenewBlockLease;
using Pkm.Application.Features.Documents.Commands.UpdateBlock;
using Pkm.Application.Features.Documents.Queries.GetBlock;
using Pkm.Application.Features.Documents.Queries.GetBlockLease;
using Pkm.Application.Features.Documents.Queries.ListPageBlocks;
using AppBlockLeaseDto = Pkm.Application.Features.Documents.Models.BlockLeaseDto;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class BlocksController : BaseController
{
    private readonly GetBlockHandler _getBlockHandler;
    private readonly GetBlockLeaseHandler _getBlockLeaseHandler;
    private readonly ListPageBlocksHandler _listPageBlocksHandler;
    private readonly CreateBlockHandler _createBlockHandler;
    private readonly UpdateBlockHandler _updateBlockHandler;
    private readonly MoveBlockHandler _moveBlockHandler;
    private readonly DeleteBlockHandler _deleteBlockHandler;
    private readonly AcquireBlockLeaseHandler _acquireBlockLeaseHandler;
    private readonly RenewBlockLeaseHandler _renewBlockLeaseHandler;
    private readonly ReleaseBlockLeaseHandler _releaseBlockLeaseHandler;

    public BlocksController(
        ICurrentUser currentUser,
        GetBlockHandler getBlockHandler,
        GetBlockLeaseHandler getBlockLeaseHandler,
        ListPageBlocksHandler listPageBlocksHandler,
        CreateBlockHandler createBlockHandler,
        UpdateBlockHandler updateBlockHandler,
        MoveBlockHandler moveBlockHandler,
        DeleteBlockHandler deleteBlockHandler,
        AcquireBlockLeaseHandler acquireBlockLeaseHandler,
        RenewBlockLeaseHandler renewBlockLeaseHandler,
        ReleaseBlockLeaseHandler releaseBlockLeaseHandler)
        : base(currentUser)
    {
        _getBlockHandler = getBlockHandler;
        _getBlockLeaseHandler = getBlockLeaseHandler;
        _listPageBlocksHandler = listPageBlocksHandler;
        _createBlockHandler = createBlockHandler;
        _updateBlockHandler = updateBlockHandler;
        _moveBlockHandler = moveBlockHandler;
        _deleteBlockHandler = deleteBlockHandler;
        _acquireBlockLeaseHandler = acquireBlockLeaseHandler;
        _renewBlockLeaseHandler = renewBlockLeaseHandler;
        _releaseBlockLeaseHandler = releaseBlockLeaseHandler;
    }

    [HttpGet("api/v1/blocks/{blockId:guid}")]
    [ProducesResponseType(typeof(ApiResult<BlockResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<BlockResponse>>> GetById(
        [FromRoute] Guid blockId,
        CancellationToken cancellationToken)
    {
        var result = await _getBlockHandler.HandleAsync(
            new GetBlockQuery(blockId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/blocks/{blockId:guid}/edit-lease")]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<BlockLeaseResponse>>> GetLease(
        [FromRoute] Guid blockId,
        CancellationToken cancellationToken)
    {
        var result = await _getBlockLeaseHandler.HandleAsync(
            new GetBlockLeaseQuery(blockId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/blocks/{blockId:guid}:acquire-edit-lease")]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 409)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<BlockLeaseResponse>>> AcquireLease(
        [FromRoute] Guid blockId,
        [FromBody] AcquireBlockLeaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _acquireBlockLeaseHandler.HandleAsync(
            new AcquireBlockLeaseCommand(
                blockId,
                request.EditorSessionId,
                request.HolderDisplayName),
            cancellationToken);

        return HandleLeaseResult(result);
    }

    [HttpPost("api/v1/blocks/{blockId:guid}:renew-edit-lease")]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 409)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<BlockLeaseResponse>>> RenewLease(
        [FromRoute] Guid blockId,
        [FromBody] RenewBlockLeaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _renewBlockLeaseHandler.HandleAsync(
            new RenewBlockLeaseCommand(
                blockId,
                request.EditorSessionId),
            cancellationToken);

        return HandleLeaseResult(result);
    }

    [HttpPost("api/v1/blocks/{blockId:guid}:release-edit-lease")]
    [ProducesResponseType(typeof(ApiResult<BlockLeaseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    public async Task<ActionResult<ApiResult<BlockLeaseResponse>>> ReleaseLease(
        [FromRoute] Guid blockId,
        [FromBody] ReleaseBlockLeaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _releaseBlockLeaseHandler.HandleAsync(
            new ReleaseBlockLeaseCommand(
                blockId,
                request.EditorSessionId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/blocks")]
    [ProducesResponseType(typeof(ApiResult<PageDocumentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PageDocumentResponse>>> GetByPage(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await _listPageBlocksHandler.HandleAsync(
            new ListPageBlocksQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/pages/{pageId:guid}/blocks")]
    [ProducesResponseType(typeof(ApiResult<BlockMutationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<BlockMutationResponse>>> Create(
        [FromRoute] Guid pageId,
        [FromBody] CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBlockCommand(
            pageId,
            request.ExpectedRevision,
            request.Type,
            request.TextContent,
            request.PropsJson,
            request.ParentBlockId,
            request.PreviousBlockId,
            request.NextBlockId,
            request.SchemaVersion);

        var result = await _createBlockHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("api/v1/blocks/{blockId:guid}")]
    [ProducesResponseType(typeof(ApiResult<BlockMutationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<BlockMutationResponse>>> Update(
        [FromRoute] Guid blockId,
        [FromBody] UpdateBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBlockCommand(
            blockId,
            request.ExpectedRevision,
            request.EditorSessionId,
            request.TextContent,
            request.PropsJson,
            request.Type);

        var result = await _updateBlockHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/blocks/{blockId:guid}:move")]
    [ProducesResponseType(typeof(ApiResult<BlockMutationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<BlockMutationResponse>>> Move(
        [FromRoute] Guid blockId,
        [FromBody] MoveBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MoveBlockCommand(
            blockId,
            request.ExpectedRevision,
            request.EditorSessionId,
            request.NewParentBlockId,
            request.PreviousBlockId,
            request.NextBlockId);

        var result = await _moveBlockHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/blocks/{blockId:guid}")]
    [ProducesResponseType(typeof(ApiResult<BlockMutationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<BlockMutationResponse>>> Delete(
        [FromRoute] Guid blockId,
        [FromQuery] long expectedRevision,
        [FromQuery] string? note,
        [FromHeader(Name = "X-Editor-Session-Id")] string? editorSessionId,
        CancellationToken cancellationToken)
    {
        var result = await _deleteBlockHandler.HandleAsync(
            new DeleteBlockCommand(
                blockId,
                expectedRevision,
                editorSessionId ?? string.Empty,
                note),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    private ActionResult<ApiResult<BlockLeaseResponse>> HandleLeaseResult(Result<AppBlockLeaseDto> result)
    {
        if (!result.IsSuccess)
            return HandleResult<AppBlockLeaseDto, BlockLeaseResponse>(result, x => x.ToResponse());

        var statusCode = result.Value.Granted
            ? StatusCodes.Status200OK
            : StatusCodes.Status409Conflict;

        return StatusCode(
            statusCode,
            ApiResult<BlockLeaseResponse>.Success(
                result.Value.ToResponse(),
                statusCode: statusCode,
                traceId: HttpContext.TraceIdentifier));
    }
}