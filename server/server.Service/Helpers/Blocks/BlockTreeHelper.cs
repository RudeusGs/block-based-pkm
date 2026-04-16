using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Helpers.Blocks
{
    public static class BlockTreeHelper
    {
        public static async Task EnsureNoCycleAsync(
            DataContext dataContext,
            int blockId,
            int? newParentBlockId,
            CancellationToken ct)
        {
            if (!newParentBlockId.HasValue)
                return;

            if (newParentBlockId.Value == blockId)
                throw new DomainException("Không thể move block vào chính nó.");

            int? cursor = newParentBlockId;

            while (cursor.HasValue)
            {
                if (cursor.Value == blockId)
                    throw new DomainException("Không thể move block vào con cháu của chính nó.");

                cursor = await dataContext.Set<Block>()
                    .AsNoTracking()
                    .Where(b => b.Id == cursor.Value && !b.IsDeleted)
                    .Select(b => b.ParentBlockId)
                    .FirstOrDefaultAsync(ct);
            }
        }

        public static async Task<List<int>> SoftDeleteSubtreeAsync(
            DataContext dataContext,
            Block rootBlock,
            CancellationToken ct)
        {
            var pageBlocks = await dataContext.Set<Block>()
                .Where(b => b.PageId == rootBlock.PageId && !b.IsDeleted)
                .ToListAsync(ct);

            var childrenMap = pageBlocks
                .GroupBy(b => b.ParentBlockId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var deletedIds = new List<int>();
            var stack = new Stack<Block>();
            stack.Push(rootBlock);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current.IsDeleted)
                    continue;

                current.SoftDelete();
                deletedIds.Add(current.Id);

                if (childrenMap.TryGetValue(current.Id, out var children))
                {
                    foreach (var child in children)
                        stack.Push(child);
                }
            }

            return deletedIds;
        }
    }
}
