using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Queries;

public sealed class ListMessagesHandler
    : IQueryHandler<ListMessagesQuery, MessagePagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;

    public ListMessagesHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
    }

    public async Task<Result<MessagePagedResultDto>> HandleAsync(
        ListMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessagePagedResultDto>(MessagingErrors.MissingUserContext);

        var conversation = await _messagingReadRepository.GetConversationForParticipantAsync(
            query.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure<MessagePagedResultDto>(MessagingErrors.ConversationForbidden);

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize, defaultPageSize: 30);
        var messages = await _messagingReadRepository.ListMessagesAsync(
            query.ConversationId,
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);
        var total = await _messagingReadRepository.CountMessagesAsync(query.ConversationId, cancellationToken);

        return Result.Success(new MessagePagedResultDto(
            messages,
            page.PageNumber,
            page.PageSize,
            total,
            PageRequest.CalculateTotalPages(total, page.PageSize)));
    }
}
