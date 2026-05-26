using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRPageRealtimePublisher : IPageRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRPageRealtimePublisher(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToWorkspaceAsync(
        PageRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Workspace(envelope.WorkspaceId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public Task PublishToPageAsync(
        PageRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope.PageId == Guid.Empty)
            return PublishToWorkspaceAsync(envelope, cancellationToken);

        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Page(envelope.PageId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public Task PublishToUserAsync(
        PageUserRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .User(envelope.UserId.ToString("D"))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}
