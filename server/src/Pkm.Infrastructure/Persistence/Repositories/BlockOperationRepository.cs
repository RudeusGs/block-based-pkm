using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Blocks;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class BlockOperationRepository : IBlockOperationRepository
{
    private readonly DataContext _dbContext;

    public BlockOperationRepository(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(BlockOperation operation)
        => _dbContext.BlockOperations.Add(operation);

    public async Task<IReadOnlyList<BlockOperation>> ListByPageAsync(
        Guid pageId,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = take <= 0 ? 100 : Math.Min(take, 500);

        return await _dbContext.BlockOperations
            .AsNoTracking()
            .Where(x => x.PageId == pageId)
            .OrderByDescending(x => x.AppliedRevision)
            .ThenByDescending(x => x.CreatedDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}