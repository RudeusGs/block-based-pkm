using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Messaging;
using Pkm.Domain.Social;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class MessagingRepository : IMessagingReadRepository, IMessagingWriteRepository
{
    private readonly DataContext _context;

    public MessagingRepository(DataContext context)
    {
        _context = context;
    }

    public Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default)
    {
        var pair = Friendship.OrderPair(userAId, userBId);
        return _context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FirstUserId == pair.First && x.SecondUserId == pair.Second, cancellationToken);
    }

    public Task<Conversation?> GetConversationForParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == conversationId && (x.FirstUserId == userId || x.SecondUserId == userId), cancellationToken);
    }

    public Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .FirstOrDefaultAsync(x => x.Id == conversationId && (x.FirstUserId == userId || x.SecondUserId == userId), cancellationToken);
    }

    public async Task<ConversationDto?> GetConversationDtoAsync(
        Guid conversationId,
        Guid viewerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(x => x.Id == conversationId && (x.FirstUserId == viewerUserId || x.SecondUserId == viewerUserId))
            .Select(x => new
            {
                Conversation = x,
                OtherUserId = x.FirstUserId == viewerUserId ? x.SecondUserId : x.FirstUserId,
                UnreadCount = _context.Messages.Count(m =>
                    m.ConversationId == x.Id &&
                    m.RecipientUserId == viewerUserId &&
                    m.ReadAtUtc == null &&
                    !m.IsDeletedForEveryone)
            })
            .Join(
                _context.Users.AsNoTracking(),
                x => x.OtherUserId,
                u => u.Id,
                (x, u) => new ConversationDto(
                    x.Conversation.Id,
                    new UserSummaryDto(u.Id, u.UserName, u.FullName, u.AvatarUrl),
                    x.Conversation.LastMessagePreview,
                    x.Conversation.LastMessageAtUtc,
                    x.UnreadCount,
                    x.Conversation.CreatedDate,
                    x.Conversation.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        return await _context.Conversations
            .AsNoTracking()
            .Where(x => x.FirstUserId == userId || x.SecondUserId == userId)
            .Select(x => new
            {
                ConversationId = x.Id,
                OtherUserId = x.FirstUserId == userId ? x.SecondUserId : x.FirstUserId,
                x.LastMessagePreview,
                x.LastMessageAtUtc,
                x.CreatedDate,
                x.UpdatedDate,
                UnreadCount = _context.Messages.Count(m =>
                    m.ConversationId == x.Id &&
                    m.RecipientUserId == userId &&
                    m.ReadAtUtc == null &&
                    !m.IsDeletedForEveryone)
            })
            .Join(
                _context.Users.AsNoTracking(),
                x => x.OtherUserId,
                u => u.Id,
                (x, u) => new
                {
                    x.ConversationId,
                    OtherUserId = u.Id,
                    u.UserName,
                    u.FullName,
                    u.AvatarUrl,
                    x.LastMessagePreview,
                    x.LastMessageAtUtc,
                    x.UnreadCount,
                    x.CreatedDate,
                    x.UpdatedDate,
                    SortDate = x.LastMessageAtUtc ?? x.CreatedDate
                })
            .OrderByDescending(x => x.SortDate)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new ConversationDto(
                x.ConversationId,
                new UserSummaryDto(x.OtherUserId, x.UserName, x.FullName, x.AvatarUrl),
                x.LastMessagePreview,
                x.LastMessageAtUtc,
                x.UnreadCount,
                x.CreatedDate,
                x.UpdatedDate))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Conversations
            .AsNoTracking()
            .CountAsync(x => x.FirstUserId == userId || x.SecondUserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> ListMessagesAsync(
        Guid conversationId,
        Guid viewerUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 30 : Math.Min(pageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        var rows = await _context.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && !x.IsDeletedForEveryone)
            .OrderByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new MessageRow(
                x.Id,
                x.ConversationId,
                x.SenderUserId,
                x.RecipientUserId,
                x.Type,
                x.Body,
                x.ImageUrl,
                x.AttachmentFileId,
                x.SenderUserId == viewerUserId,
                x.ReadAtUtc,
                x.CreatedDate,
                x.UpdatedDate))
            .ToListAsync(cancellationToken);

        return await BuildMessageDtosAsync(rows, viewerUserId, cancellationToken);
    }

    public async Task<MessageDto?> GetMessageDtoAsync(
        Guid messageId,
        Guid viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.Messages
            .AsNoTracking()
            .Where(x =>
                x.Id == messageId &&
                !x.IsDeletedForEveryone &&
                _context.Conversations.Any(c =>
                    c.Id == x.ConversationId &&
                    (c.FirstUserId == viewerUserId || c.SecondUserId == viewerUserId)))
            .Select(x => new MessageRow(
                x.Id,
                x.ConversationId,
                x.SenderUserId,
                x.RecipientUserId,
                x.Type,
                x.Body,
                x.ImageUrl,
                x.AttachmentFileId,
                x.SenderUserId == viewerUserId,
                x.ReadAtUtc,
                x.CreatedDate,
                x.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return (await BuildMessageDtosAsync(new[] { row }, viewerUserId, cancellationToken)).FirstOrDefault();
    }

    public Task<Message?> GetMessageForRecipientAsync(
        Guid messageId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                x.RecipientUserId == recipientUserId &&
                !x.IsDeletedForEveryone,
                cancellationToken);
    }

    public Task<Message?> GetMessageForParticipantForUpdateAsync(
        Guid messageId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                !x.IsDeletedForEveryone &&
                _context.Conversations.Any(c =>
                    c.Id == x.ConversationId &&
                    (c.FirstUserId == userId || c.SecondUserId == userId)),
                cancellationToken);
    }

    public Task<int> CountMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .AsNoTracking()
            .CountAsync(x => x.ConversationId == conversationId && !x.IsDeletedForEveryone, cancellationToken);
    }

    public async Task<int> MarkConversationReadAsync(
        Guid conversationId,
        Guid readerUserId,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken = default)
    {
        var messages = await _context.Messages
            .Where(x =>
                x.ConversationId == conversationId &&
                x.RecipientUserId == readerUserId &&
                x.ReadAtUtc == null &&
                !x.IsDeletedForEveryone)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
            message.MarkRead(readAtUtc);

        return messages.Count;
    }

    public Task<MessageReaction?> GetReactionForUserForUpdateAsync(
        Guid messageId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.MessageReactions
            .FirstOrDefaultAsync(x => x.MessageId == messageId && x.UserId == userId, cancellationToken);
    }

    public Task<MessagePin?> GetPinForMessageForUpdateAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return _context.MessagePins
            .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
    }

    private async Task<IReadOnlyList<MessageDto>> BuildMessageDtosAsync(
        IReadOnlyList<MessageRow> rows,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return Array.Empty<MessageDto>();

        var messageIds = rows.Select(x => x.Id).ToArray();

        var reactionRows = await _context.MessageReactions
            .AsNoTracking()
            .Where(x => messageIds.Contains(x.MessageId))
            .GroupBy(x => new { x.MessageId, x.Emoji })
            .Select(x => new
            {
                x.Key.MessageId,
                x.Key.Emoji,
                Count = x.Count(),
                ReactedByMe = x.Any(y => y.UserId == viewerUserId)
            })
            .ToListAsync(cancellationToken);

        var reactionsByMessage = reactionRows
            .GroupBy(x => x.MessageId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<MessageReactionDto>)x
                    .OrderByDescending(y => y.Count)
                    .ThenBy(y => y.Emoji)
                    .Select(y => new MessageReactionDto(y.Emoji, y.Count, y.ReactedByMe))
                    .ToArray());

        var pinnedIds = await _context.MessagePins
            .AsNoTracking()
            .Where(x => messageIds.Contains(x.MessageId))
            .Select(x => x.MessageId)
            .ToListAsync(cancellationToken);
        var pinned = pinnedIds.ToHashSet();

        return rows.Select(x => new MessageDto(
                x.Id,
                x.ConversationId,
                x.SenderUserId,
                x.RecipientUserId,
                x.Type,
                x.Body,
                x.ImageUrl,
                x.AttachmentFileId,
                x.IsMine,
                x.ReadAtUtc,
                x.CreatedDate,
                x.UpdatedDate,
                reactionsByMessage.TryGetValue(x.Id, out var reactions) ? reactions : Array.Empty<MessageReactionDto>(),
                pinned.Contains(x.Id)))
            .ToArray();
    }

    public void AddConversation(Conversation conversation) => _context.Conversations.Add(conversation);

    public void AddMessage(Message message) => _context.Messages.Add(message);

    public void AddReaction(MessageReaction reaction) => _context.MessageReactions.Add(reaction);

    public void AddPin(MessagePin pin) => _context.MessagePins.Add(pin);

    public void RemoveReaction(MessageReaction reaction) => _context.MessageReactions.Remove(reaction);

    public void RemovePin(MessagePin pin) => _context.MessagePins.Remove(pin);

    public void UpdateConversation(Conversation conversation) => _context.Conversations.Update(conversation);

    public void UpdateMessage(Message message) => _context.Messages.Update(message);

    public void UpdateReaction(MessageReaction reaction) => _context.MessageReactions.Update(reaction);

    private sealed record MessageRow(
        Guid Id,
        Guid ConversationId,
        Guid SenderUserId,
        Guid RecipientUserId,
        MessageType Type,
        string? Body,
        string? ImageUrl,
        Guid? AttachmentFileId,
        bool IsMine,
        DateTimeOffset? ReadAtUtc,
        DateTimeOffset CreatedDate,
        DateTimeOffset? UpdatedDate);
}
