namespace Pkm.Infrastructure.Persistence.Outbox;

internal interface IOutboxMessageDispatcher
{
    Task DispatchAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);
}
