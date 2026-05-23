using Pkm.Application.Common.Results;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Messaging.Services;

public interface IMessagingApplicationService
{
    Task<Result<ConversationDto>> CreateOrGetDirectConversationAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<Result<ConversationPagedResultDto>> ListConversationsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<MessagePagedResultDto>> ListMessagesAsync(Guid conversationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<MessageDto>> SendTextMessageAsync(Guid conversationId, string body, CancellationToken cancellationToken = default);

    Task<Result<MessageDto>> SendImageMessageAsync(Guid conversationId, string? caption, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken = default);

    Task<Result<MessageDto>> SendWorkspaceShareMessageAsync(Guid conversationId, Guid workspaceId, string role, CancellationToken cancellationToken = default);

    Task<Result<WorkspaceDto>> AcceptWorkspaceShareAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<Result> MarkConversationReadAsync(Guid conversationId, CancellationToken cancellationToken = default);
}



