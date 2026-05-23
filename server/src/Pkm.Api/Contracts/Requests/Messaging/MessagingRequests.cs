namespace Pkm.Api.Contracts.Requests.Messaging;

public sealed record CreateDirectConversationRequest(
    Guid RecipientUserId);

public sealed record SendTextMessageRequest(
    string Body);




public sealed record SendWorkspaceShareMessageRequest(
    Guid WorkspaceId,
    string? Role);
