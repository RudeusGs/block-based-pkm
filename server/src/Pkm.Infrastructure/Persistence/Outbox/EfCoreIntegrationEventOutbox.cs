using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Time;

namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class EfCoreIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly DataContext _dbContext;
    private readonly IOutboxMessageSerializer _serializer;
    private readonly IClock _clock;

    public EfCoreIntegrationEventOutbox(
        DataContext dbContext,
        IOutboxMessageSerializer serializer,
        IClock clock)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _clock = clock;
    }

    public ValueTask EnqueueAsync(
        IntegrationEventEnvelope integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var outboxMessage = OutboxMessage.Create(
            eventType: integrationEvent.Type,
            source: integrationEvent.Source,
            schemaVersion: integrationEvent.SchemaVersion,
            payloadJson: _serializer.Serialize(integrationEvent.Payload),
            occurredAtUtc: integrationEvent.OccurredAtUtc,
            createdAtUtc: _clock.UtcNow,
            traceId: integrationEvent.TraceId,
            correlationId: integrationEvent.CorrelationId);

        _dbContext.OutboxMessages.Add(outboxMessage);
        return ValueTask.CompletedTask;
    }
}
