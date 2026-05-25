using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Messaging.Services;

namespace Pkm.Application.Features.Messaging.Queries;

public sealed record ListConversationsQuery(int PageNumber, int PageSize)
    : IQuery<ConversationPagedResultDto>;

public sealed class ListConversationsHandler
    : IQueryHandler<ListConversationsQuery, ConversationPagedResultDto>
{
    private readonly IMessagingQueryService _messagingQueryService;

    public ListConversationsHandler(IMessagingQueryService messagingQueryService)
    {
        _messagingQueryService = messagingQueryService;
    }

    public Task<Result<ConversationPagedResultDto>> HandleAsync(
        ListConversationsQuery query,
        CancellationToken cancellationToken = default)
        => _messagingQueryService.ListConversationsAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}

public sealed record ListMessagesQuery(
    Guid ConversationId,
    int PageNumber,
    int PageSize) : IQuery<MessagePagedResultDto>;

public sealed class ListMessagesHandler
    : IQueryHandler<ListMessagesQuery, MessagePagedResultDto>
{
    private readonly IMessagingQueryService _messagingQueryService;

    public ListMessagesHandler(IMessagingQueryService messagingQueryService)
    {
        _messagingQueryService = messagingQueryService;
    }

    public Task<Result<MessagePagedResultDto>> HandleAsync(
        ListMessagesQuery query,
        CancellationToken cancellationToken = default)
        => _messagingQueryService.ListMessagesAsync(
            query.ConversationId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}
