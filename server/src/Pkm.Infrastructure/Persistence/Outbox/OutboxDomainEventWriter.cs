using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Domain.SharedKernel;

namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class OutboxDomainEventWriter : IOutboxDomainEventWriter
{
    private readonly DataContext _dbContext;
    private readonly IOutboxMessageSerializer _serializer;
    private readonly IClock _clock;

    public OutboxDomainEventWriter(
        DataContext dbContext,
        IOutboxMessageSerializer serializer,
        IClock clock)
    {
        _dbContext = dbContext;
        _serializer = serializer;
        _clock = clock;
    }

    public void EnqueueDomainEvents()
    {
        var entitiesWithEvents = _dbContext.ChangeTracker
            .Entries<EntityBase>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .Distinct()
            .ToArray();

        if (entitiesWithEvents.Length == 0)
            return;

        var domainEvents = entitiesWithEvents
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var outboxMessage = OutboxMessage.Create(
                eventType: eventType.FullName ?? eventType.Name,
                source: $"{IntegrationEventEnvelope.DefaultSource}.domain",
                schemaVersion: IntegrationEventEnvelope.DefaultSchemaVersion,
                payloadJson: _serializer.Serialize(domainEvent),
                occurredAtUtc: domainEvent.OccurredAtUtc,
                createdAtUtc: _clock.UtcNow);

            _dbContext.OutboxMessages.Add(outboxMessage);
        }
    }
}
