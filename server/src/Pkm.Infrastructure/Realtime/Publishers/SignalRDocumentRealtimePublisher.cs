using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Infrastructure.Realtime.Hubs;

namespace Pkm.Infrastructure.Realtime.Publishers;

public sealed class SignalRDocumentRealtimePublisher : IDocumentRealtimePublisher
{
    private readonly IHubContext<CollaborationHub> _hubContext;

    public SignalRDocumentRealtimePublisher(IHubContext<CollaborationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToWorkspaceAsync(
        DocumentRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Workspace(envelope.WorkspaceId))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }

    public Task PublishToPageAsync(
        DocumentRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope.PageId is null || envelope.PageId == Guid.Empty)
            throw new InvalidOperationException("PageId is required for page-scoped realtime publishing.");

        return _hubContext
            .Clients
            .Group(RealtimeGroupNames.Page(envelope.PageId.Value))
            .SendAsync(envelope.EventName, envelope, cancellationToken);
    }
}