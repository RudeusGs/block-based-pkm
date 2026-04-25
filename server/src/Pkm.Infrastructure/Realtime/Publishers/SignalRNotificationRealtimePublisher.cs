using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRNotificationRealtimePublisher(
        IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToUserAsync(
        NotificationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .User(envelope.UserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}