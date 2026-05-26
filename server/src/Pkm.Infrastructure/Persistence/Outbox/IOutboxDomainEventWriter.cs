namespace Pkm.Infrastructure.Persistence.Outbox;

internal interface IOutboxDomainEventWriter
{
    void EnqueueDomainEvents();
}
