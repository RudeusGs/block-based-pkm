using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Messaging;
using Pkm.Api.Contracts.Responses.Messaging;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Messaging.Services;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/conversations")]
public sealed class ConversationsController : BaseController
{
    private const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;

    private readonly IMessagingApplicationService _messagingApplicationService;

    public ConversationsController(
        ICurrentUser currentUser,
        IMessagingApplicationService messagingApplicationService)
        : base(currentUser)
    {
        _messagingApplicationService = messagingApplicationService;
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
        var result = await _messagingApplicationService.CreateOrGetDirectConversationAsync(
            request.RecipientUserId,
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
        var result = await _messagingApplicationService.ListConversationsAsync(
            pageNumber,
            pageSize,
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
        var result = await _messagingApplicationService.ListMessagesAsync(
            conversationId,
            pageNumber,
            pageSize,
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
        var result = await _messagingApplicationService.SendTextMessageAsync(
            conversationId,
            request.Body,
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
        var result = await _messagingApplicationService.SendWorkspaceShareMessageAsync(
            conversationId,
            request.WorkspaceId,
            request.Role ?? "viewer",
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
        var result = await _messagingApplicationService.AcceptWorkspaceShareAsync(
            messageId,
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

        var result = await _messagingApplicationService.SendImageMessageAsync(
            conversationId,
            request.Caption,
            file.FileName,
            ResolveContentType(file),
            file.Length,
            stream,
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
        var result = await _messagingApplicationService.MarkConversationReadAsync(
            conversationId,
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

public sealed class SendImageMessageFormRequest
{
    public IFormFile? File { get; init; }
    public string? Caption { get; init; }
}
