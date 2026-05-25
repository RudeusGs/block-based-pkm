using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Messaging;
using Pkm.Api.Contracts.Responses.Messaging;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Commands;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Messaging.Queries;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController : BaseController
{
    private const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;

    private readonly IUseCaseDispatcher _useCaseDispatcher;

    public ConversationsController(
        ICurrentUser currentUser,
        IUseCaseDispatcher useCaseDispatcher)
        : base(currentUser)
    {
        _useCaseDispatcher = useCaseDispatcher;
    }

    [HttpPost("direct")]
    [ProducesResponseType(typeof(ApiResult<ConversationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<ConversationResponse>>> CreateOrGetDirectConversation(
        [FromBody] CreateDirectConversationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<CreateDirectConversationCommand, ConversationDto>(
            new CreateDirectConversationCommand(request.RecipientUserId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<ConversationPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<ConversationPagedResultResponse>>> ListConversations(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.QueryAsync<ListConversationsQuery, ConversationPagedResultDto>(
            new ListConversationsQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResult<MessagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    public async Task<ActionResult<ApiResult<MessagePagedResultResponse>>> ListMessages(
        [FromRoute] Guid conversationId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.QueryAsync<ListMessagesQuery, MessagePagedResultDto>(
            new ListMessagesQuery(conversationId, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> SendTextMessage(
        [FromRoute] Guid conversationId,
        [FromBody] SendTextMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<SendTextMessageCommand, MessageDto>(
            new SendTextMessageCommand(conversationId, request.Body),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("{conversationId:guid}/messages/workspace-share")]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> SendWorkspaceShareMessage(
        [FromRoute] Guid conversationId,
        [FromBody] SendWorkspaceShareMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<SendWorkspaceShareMessageCommand, MessageDto>(
            new SendWorkspaceShareMessageCommand(
                conversationId,
                request.WorkspaceId,
                request.Role ?? "viewer"),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("messages/{messageId:guid}/workspace-share/accept")]
    [ProducesResponseType(typeof(ApiResult<WorkspaceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkspaceResponse>>> AcceptWorkspaceShare(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<AcceptWorkspaceShareCommand, WorkspaceDto>(
            new AcceptWorkspaceShareCommand(messageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> DeleteMessage(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync(
            new DeleteMessageForEveryoneCommand(messageId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("messages/{messageId:guid}/reactions")]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> ToggleMessageReaction(
        [FromRoute] Guid messageId,
        [FromBody] SetMessageReactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<ToggleMessageReactionCommand, MessageDto>(
            new ToggleMessageReactionCommand(messageId, request.Emoji),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("messages/{messageId:guid}/pin")]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> PinMessage(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<PinMessageCommand, MessageDto>(
            new PinMessageCommand(messageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("messages/{messageId:guid}/pin")]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> UnpinMessage(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync<UnpinMessageCommand, MessageDto>(
            new UnpinMessageCommand(messageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("{conversationId:guid}/messages/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBodySizeBytes)]
    [ProducesResponseType(typeof(ApiResult<MessageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 413)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<MessageResponse>>> SendImageMessage(
        [FromRoute] Guid conversationId,
        [FromForm] SendImageMessageFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidFile(request.File))
            return BadRequest(CreateFileRequiredFailure<MessageResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var result = await _useCaseDispatcher.ExecuteAsync<SendImageMessageCommand, MessageDto>(
            new SendImageMessageCommand(
                conversationId,
                request.Caption,
                file.FileName,
                ResolveContentType(file),
                file.Length,
                stream),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("{conversationId:guid}/read")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    public async Task<ActionResult<ApiResult>> MarkRead(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await _useCaseDispatcher.ExecuteAsync(
            new MarkConversationReadCommand(conversationId),
            cancellationToken);

        return HandleResult(result);
    }

    private static bool HasValidFile(IFormFile? file)
        => file is not null && file.Length > 0;

    private static ApiResult<TResponse> CreateFileRequiredFailure<TResponse>()
        => ApiResult<TResponse>.Failure(
            message: "Vui lòng chọn file ảnh để upload.",
            error: new ApiError(
                Code: "File.Required",
                Type: "validation_error",
                Details: new[] { "Trường file là bắt buộc." }),
            statusCode: StatusCodes.Status400BadRequest);

    private static string ResolveContentType(IFormFile file)
        => string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
}
