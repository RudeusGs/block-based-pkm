using server.Domain.Entities;
using server.Domain.Enums;
using server.Service.Models.Block;

namespace server.Application.Common.Helpers.Blocks
{
    public static class BlockMapper
    {
        public static bool TryParseBlockType(string? rawType, out BlockType blockType)
        {
            if (string.IsNullOrWhiteSpace(rawType))
            {
                blockType = default;
                return false;
            }

            return Enum.TryParse(rawType.Trim(), ignoreCase: true, out blockType);
        }

        public static BlockResponseModel ToResponse(Block block)
        {
            return new BlockResponseModel
            {
                Id = block.Id,
                PageId = block.PageId,
                ParentBlockId = block.ParentBlockId,
                Type = block.Type.ToString(),
                TextContent = block.TextContent,
                PropsJson = block.PropsJson,
                OrderKey = block.OrderKey,
                CreatedBy = block.CreatedBy,
                LastModifiedBy = block.LastModifiedBy,
                CreatedDate = block.CreatedDate,
                UpdatedDate = block.UpdatedDate,
                RowVersion = block.RowVersion == null || block.RowVersion.Length == 0
                    ? null
                    : Convert.ToBase64String(block.RowVersion)
            };
        }
    }
}
