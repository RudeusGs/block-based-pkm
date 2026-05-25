using Microsoft.AspNetCore.Http;

namespace Pkm.Api.Contracts.Requests.Messaging;

public sealed record CreateDirectConversationRequest(
    Guid RecipientUserId);

public sealed record SendTextMessageRequest(
    string Body);

public sealed record SetMessageReactionRequest(
    string Emoji);

public sealed record SendWorkspaceShareMessageRequest(
    Guid WorkspaceId,
    string? Role);

public sealed class SendImageMessageFormRequest
{
    public IFormFile? File { get; init; }
    public string? Caption { get; init; }
}
