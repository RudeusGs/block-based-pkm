using Microsoft.EntityFrameworkCore;
using server.Application.Models.Internal;
using server.Domain.Base;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;

namespace server.Application.Common.Helpers.Blocks
{
    public static class BlockAccessHelper
    {
        public static async Task<PageAccessContext> GetPageAccessContextAsync(
            DataContext dataContext,
            IUserService userService,
            IPermissionService permissionService,
            int pageId,
            CancellationToken ct)
        {
            var userId = ServiceHelper.GetCurrentUserIdOrThrow(userService);

            var page = await dataContext.Set<Page>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == pageId &&
                    !p.IsDeleted &&
                    !p.IsArchived, ct);

            if (page == null)
                throw new DomainException("Page không tồn tại hoặc đã bị archive.");

            var hasAccess = await permissionService.HasWorkspaceAccessAsync(page.WorkspaceId, userId, ct);
            if (!hasAccess)
                throw new UnauthorizedAccessException("Không có quyền truy cập page.");

            return new PageAccessContext(page, userId);
        }

        public static async Task<BlockAccessContext> GetBlockAccessContextAsync(
            DataContext dataContext,
            IUserService userService,
            IPermissionService permissionService,
            int blockId,
            CancellationToken ct)
        {
            var userId = ServiceHelper.GetCurrentUserIdOrThrow(userService);

            var block = await dataContext.Set<Block>()
                .FirstOrDefaultAsync(b => b.Id == blockId && !b.IsDeleted, ct);

            if (block == null)
                throw new DomainException("Block không tồn tại.");

            var page = await dataContext.Set<Page>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == block.PageId &&
                    !p.IsDeleted &&
                    !p.IsArchived, ct);

            if (page == null)
                throw new DomainException("Page chứa block không tồn tại hoặc đã bị archive.");

            var hasAccess = await permissionService.HasWorkspaceAccessAsync(page.WorkspaceId, userId, ct);
            if (!hasAccess)
                throw new UnauthorizedAccessException("Không có quyền truy cập block.");

            return new BlockAccessContext(block, page, userId);
        }

        public static async Task EnsureParentBlockBelongsToPageAsync(
            DataContext dataContext,
            int pageId,
            int? parentBlockId,
            CancellationToken ct)
        {
            if (!parentBlockId.HasValue)
                return;

            var isValid = await dataContext.Set<Block>()
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Id == parentBlockId.Value &&
                    b.PageId == pageId &&
                    !b.IsDeleted, ct);

            if (!isValid)
                throw new DomainException("ParentBlock không hợp lệ hoặc không thuộc page này.");
        }
    }
}
