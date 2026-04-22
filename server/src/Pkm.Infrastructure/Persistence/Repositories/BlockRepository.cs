using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Documents.Models;
using Pkm.Domain.Blocks;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class BlockRepository : IBlockRepository
{
    private readonly DataContext _dataContext;

    public BlockRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Block?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Blocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<Block?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Blocks
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Block>> ListByPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Blocks
            .AsNoTracking()
            .Where(x => x.PageId == pageId && !x.IsDeleted)
            .OrderBy(x => x.ParentBlockId)
            .ThenBy(x => x.OrderKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Block>> ListByPageForUpdateAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Blocks
            .Where(x => x.PageId == pageId && !x.IsDeleted)
            .OrderBy(x => x.ParentBlockId)
            .ThenBy(x => x.OrderKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsDescendantOrSelfAsync(
        Guid blockId,
        Guid candidateParentBlockId,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty || candidateParentBlockId == Guid.Empty)
            return false;

        if (blockId == candidateParentBlockId)
            return true;

        Guid? current = candidateParentBlockId;

        while (current.HasValue)
        {
            if (current.Value == blockId)
                return true;

            current = await _dataContext.Blocks
                .AsNoTracking()
                .Where(x => x.Id == current.Value && !x.IsDeleted)
                .Select(x => x.ParentBlockId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    public async Task<BlockAccessContextReadModel?> GetAccessContextAsync(
    Guid blockId,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty || userId == Guid.Empty)
            return null;

        return await
            (from block in _dataContext.Blocks.AsNoTracking()
             where block.Id == blockId && !block.IsDeleted
             join page in _dataContext.Pages.AsNoTracking()
                 on block.PageId equals page.Id
             join workspace in _dataContext.Workspaces.AsNoTracking()
                 on page.WorkspaceId equals workspace.Id
             join member in _dataContext.WorkspaceMembers.AsNoTracking()
                    .Where(x => x.UserId == userId)
                 on workspace.Id equals member.WorkspaceId into memberships
             from membership in memberships.DefaultIfEmpty()
             select new BlockAccessContextReadModel(
                 block.Id,
                 page.Id,
                 page.WorkspaceId,
                 workspace.OwnerId,
                 membership != null ? membership.Role : null,
                 page.IsArchived))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Block block) => _dataContext.Blocks.Add(block);
}