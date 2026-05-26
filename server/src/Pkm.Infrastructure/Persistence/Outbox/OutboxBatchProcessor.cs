using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Time;

namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class OutboxBatchProcessor : IOutboxBatchProcessor
{
    private const int BatchSize = 25;
    private const int MaxRetries = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);
    private static readonly string DomainEventSource = $"{IntegrationEventEnvelope.DefaultSource}.domain";

    private readonly DataContext _dbContext;
    private readonly IOutboxMessageDispatcher _dispatcher;
    private readonly IClock _clock;
    private readonly ILogger<OutboxBatchProcessor> _logger;

    public OutboxBatchProcessor(
        DataContext dbContext,
        IOutboxMessageDispatcher dispatcher,
        IClock clock,
        ILogger<OutboxBatchProcessor> logger)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessNextBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var lockId = Guid.NewGuid().ToString("N");
        var lockedUntilUtc = now.Add(LockDuration);

        var candidateIds = await _dbContext.OutboxMessages
            .Where(x => x.Source == DomainEventSource)
            .Where(x =>
                x.Status == OutboxMessageStatus.Pending ||
                (x.Status == OutboxMessageStatus.Processing &&
                 x.LockedUntilUtc.HasValue &&
                 x.LockedUntilUtc.Value < now))
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => x.Id)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);

        if (candidateIds.Length == 0)
            return 0;

        var claimedCount = await _dbContext.OutboxMessages
            .Where(x => candidateIds.Contains(x.Id))
            .Where(x =>
                x.Status == OutboxMessageStatus.Pending ||
                (x.Status == OutboxMessageStatus.Processing &&
                 x.LockedUntilUtc.HasValue &&
                 x.LockedUntilUtc.Value < now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, OutboxMessageStatus.Processing)
                    .SetProperty(x => x.LockId, lockId)
                    .SetProperty(x => x.LockedUntilUtc, lockedUntilUtc),
                cancellationToken);

        if (claimedCount == 0)
            return 0;

        var messages = await _dbContext.OutboxMessages
            .Where(x => x.LockId == lockId && x.Status == OutboxMessageStatus.Processing)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _dispatcher.DispatchAsync(message, cancellationToken);
                message.MarkProcessed(_clock.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message {OutboxMessageId} with type {EventType}.",
                    message.Id,
                    message.EventType);

                message.MarkFailed(ex.Message, MaxRetries);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return messages.Count;
    }
}
