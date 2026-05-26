using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Domain.SharedKernel;
using Pkm.Infrastructure.Persistence.Outbox;

namespace Pkm.Infrastructure.Persistence.Repositories;

/// <summary>
/// Wraps DataContext.SaveChangesAsync and keeps domain-event outbox writes in the same transaction.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _dbContext;
    private readonly IOutboxDomainEventWriter _domainEventWriter;

    public UnitOfWork(
        DataContext dbContext,
        IOutboxDomainEventWriter domainEventWriter)
    {
        _dbContext = dbContext;
        _domainEventWriter = domainEventWriter;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _domainEventWriter.EnqueueDomainEvents();

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
        ClearDomainEventsAfterSuccessfulCommit();

        return affectedRows;
    }

    private void ClearDomainEventsAfterSuccessfulCommit()
    {
        var entitiesWithEvents = _dbContext.ChangeTracker
            .Entries<EntityBase>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity);

        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }
    }
}
