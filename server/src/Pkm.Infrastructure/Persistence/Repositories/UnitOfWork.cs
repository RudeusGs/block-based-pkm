using Pkm.Application.Abstractions.Persistence;

namespace Pkm.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IUnitOfWork wrap SaveChangesAsync của DataContext.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _dbContext;

    public UnitOfWork(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
