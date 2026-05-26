using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Realtime;

public static class PageRealtimePublisherExtensions
{
    public static Task PublishWorkspacePageChangedAsync(
        this IPageRealtimePublisher publisher,
        string eventName,
        PageDto page,
        Guid actorId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        return publisher.PublishToWorkspaceAsync(
            new PageRealtimeEnvelope(
                EventName: eventName,
                WorkspaceId: page.WorkspaceId,
                PageId: page.Id,
                ParentPageId: page.ParentPageId,
                ActorId: actorId,
                OccurredAtUtc: occurredAtUtc,
                Revision: page.CurrentRevision,
                Payload: page),
            cancellationToken);
    }
}
