using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRSocialRealtimePublisher : ISocialRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRSocialRealtimePublisher(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToUserAsync(
        SocialRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .User(envelope.UserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public async Task PublishToConversationAsync(
        MessagingRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        await _hubContext
            .Clients
            .Group(RealtimeGroupNames.Conversation(envelope.ConversationId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);

        await _hubContext
            .Clients
            .User(envelope.RecipientUserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);

        await _hubContext
            .Clients
            .User(envelope.SenderUserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}
