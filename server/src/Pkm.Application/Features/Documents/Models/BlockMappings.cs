using Pkm.Domain.Blocks;

namespace Pkm.Application.Features.Documents.Models;

public static class BlockMappings
{
    public static BlockDto ToDto(this Block block)
        => new(
            block.Id,
            block.PageId,
            block.ParentBlockId,
            block.Type.Value,
            block.TextContent,
            block.PropsJson,
            block.SchemaVersion,
            block.OrderKey,
            block.CreatedBy,
            block.LastModifiedBy,
            block.CreatedDate,
            block.UpdatedDate);
}   