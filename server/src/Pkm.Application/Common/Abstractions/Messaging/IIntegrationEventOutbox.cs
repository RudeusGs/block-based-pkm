namespace Pkm.Application.Common.Abstractions.Messaging;

public interface IIntegrationEventOutbox
{
    ValueTask EnqueueAsync(
        IntegrationEventEnvelope integrationEvent,
        CancellationToken cancellationToken = default);
}
