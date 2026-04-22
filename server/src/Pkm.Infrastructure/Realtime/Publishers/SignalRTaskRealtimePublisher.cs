using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRTaskRealtimePublisher : ITaskRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRTaskRealtimePublisher(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToWorkspaceAsync(
        TaskRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Workspace(envelope.WorkspaceId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public Task PublishToPageAsync(
        TaskRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope.PageId is null || envelope.PageId == Guid.Empty)
        {
            return PublishToWorkspaceAsync(envelope, cancellationToken);
        }

        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Page(envelope.PageId.Value))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}