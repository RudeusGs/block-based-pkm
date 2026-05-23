using Pkm.Application.Common.Results;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Services;

public interface IMessagingApplicationService
{
    Task<Result<ConversationDto>> CreateOrGetDirectConversationAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<Result<ConversationPagedResultDto>> ListConversationsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<MessagePagedResultDto>> ListMessagesAsync(Guid conversationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<MessageDto>> SendTextMessageAsync(Guid conversationId, string body, CancellationToken cancellationToken = default);

    Task<Result<MessageDto>> SendImageMessageAsync(Guid conversationId, string? caption, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken = default);

    Task<Result> MarkConversationReadAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
