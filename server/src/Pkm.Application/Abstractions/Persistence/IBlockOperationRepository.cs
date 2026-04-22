using Pkm.Domain.Blocks;

namespace Pkm.Application.Abstractions.Persistence;

public interface IBlockOperationRepository
{
    void Add(BlockOperation operation);

    Task<IReadOnlyList<BlockOperation>> ListByPageAsync(
        Guid pageId,
        int take,
        CancellationToken cancellationToken = default);
}