namespace Pkm.Application.Common.Abstractions.Realtime;

public interface IDocumentRealtimePublisher
{
    Task PublishToWorkspaceAsync(
        DocumentRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToPageAsync(
        DocumentRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}
