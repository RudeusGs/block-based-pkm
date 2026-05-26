using Pkm.Application.Features.Documents.Models;
using Pkm.Domain.Blocks;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IBlockReadRepository
{
    Task<Block?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListByPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListByPageAsync(
        Guid pageId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByPageAsync(
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
}

public interface IBlockWriteRepository
{
    Task<Block?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListByPageForUpdateAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> ListSiblingsForUpdateAsync(
        Guid pageId,
        Guid? parentBlockId,
        CancellationToken cancellationToken = default);

    void Add(Block block);
}
