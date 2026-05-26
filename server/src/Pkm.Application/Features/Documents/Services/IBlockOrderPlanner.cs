using Pkm.Application.Common.Results;
using Pkm.Domain.Blocks;

namespace Pkm.Application.Features.Documents.Services;

public interface IBlockOrderPlanner
{
    Result<string> CreateOrderKeyOrRebalance(
        IReadOnlyList<Block> siblings,
        Guid? previousBlockId,
        Guid? nextBlockId,
        Guid? parentBlockId,
        Guid actorId,
        DateTimeOffset now,
        Guid newBlockId);
}
