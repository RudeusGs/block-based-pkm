using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Blocks;
using Pkm.Api.Contracts.Responses.Blocks;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Commands.AcquireBlockLease;
using Pkm.Application.Features.Documents.Commands.CreateBlock;
using Pkm.Application.Features.Documents.Commands.DeleteBlock;
using Pkm.Application.Features.Documents.Commands.MoveBlock;
using Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;
using Pkm.Application.Features.Documents.Commands.RenewBlockLease;
using Pkm.Application.Features.Documents.Commands.UpdateBlock;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Queries.GetBlock;
using Pkm.Application.Features.Documents.Queries.GetBlockLease;
using Pkm.Application.Features.Documents.Queries.ListPageBlocks;
using AppBlockLeaseDto = Pkm.Application.Features.Documents.Models.BlockLeaseDto;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class BlocksController : BaseController
{
    public BlocksController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser, dispatcher)
    {
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
        var result = await QueryAsync<GetBlockQuery, BlockDto>(
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
        var result = await QueryAsync<GetBlockLeaseQuery, BlockLeaseDto>(
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
        var result = await ExecuteAsync<AcquireBlockLeaseCommand, BlockLeaseDto>(
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
        var result = await ExecuteAsync<RenewBlockLeaseCommand, BlockLeaseDto>(
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
        var result = await ExecuteAsync<ReleaseBlockLeaseCommand, BlockLeaseDto>(
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
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListPageBlocksQuery, PageDocumentDto>(
            new ListPageBlocksQuery(pageId, pageNumber, pageSize),
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

        var result = await ExecuteAsync<CreateBlockCommand, BlockMutationDto>(command, cancellationToken);
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

        var result = await ExecuteAsync<UpdateBlockCommand, BlockMutationDto>(command, cancellationToken);
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

        var result = await ExecuteAsync<MoveBlockCommand, BlockMutationDto>(command, cancellationToken);
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
        var result = await ExecuteAsync<DeleteBlockCommand, BlockMutationDto>(
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
