namespace Pkm.Infrastructure.Persistence.Outbox;

internal interface IOutboxBatchProcessor
{
    Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken = default);
}
