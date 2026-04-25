using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRRecommendationRealtimePublisher : IRecommendationRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRRecommendationRealtimePublisher(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToUserAsync(
        RecommendationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .User(envelope.UserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public Task PublishToWorkspaceAsync(
        RecommendationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Workspace(envelope.WorkspaceId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}