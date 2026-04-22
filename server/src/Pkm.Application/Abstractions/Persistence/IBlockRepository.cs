using Pkm.Application.Features.Documents.Models;
using Pkm.Domain.Blocks;

namespace Pkm.Application.Abstractions.Persistence;

public interface IBlockRepository
{
    Task<Block?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Block?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListByPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListByPageForUpdateAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<bool> IsDescendantOrSelfAsync(
        Guid blockId,
        Guid candidateParentBlockId,
        CancellationToken cancellationToken = default);

    Task<BlockAccessContextReadModel?> GetAccessContextAsync(
        Guid blockId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Block block);
}