using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Messaging;
using Pkm.Domain.Social;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class MessagingRepository : IMessagingRepository
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
                Conversation = x,
                OtherUserId = x.FirstUserId == userId ? x.SecondUserId : x.FirstUserId,
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
                    ConversationId = x.Conversation.Id,
                    OtherUserId = u.Id,
                    u.UserName,
                    u.FullName,
                    u.AvatarUrl,
                    x.Conversation.LastMessagePreview,
                    x.Conversation.LastMessageAtUtc,
                    x.UnreadCount,
                    x.Conversation.CreatedDate,
                    x.Conversation.UpdatedDate,
                    SortDate = x.Conversation.LastMessageAtUtc ?? x.Conversation.CreatedDate
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

        return await _context.Messages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && !x.IsDeletedForEveryone)
            .OrderByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new MessageDto(
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
    }

    public Task<MessageDto?> GetMessageDtoAsync(
        Guid messageId,
        Guid viewerUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .AsNoTracking()
            .Where(x =>
                x.Id == messageId &&
                !x.IsDeletedForEveryone &&
                _context.Conversations.Any(c =>
                    c.Id == x.ConversationId &&
                    (c.FirstUserId == viewerUserId || c.SecondUserId == viewerUserId)))
            .Select(x => new MessageDto(
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

    public void AddConversation(Conversation conversation) => _context.Conversations.Add(conversation);

    public void AddMessage(Message message) => _context.Messages.Add(message);

    public void UpdateConversation(Conversation conversation) => _context.Conversations.Update(conversation);
}
