using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.Page;
using server.Domain.Entities;

namespace server.Service.Services
{
    public class PageService : BaseService, IPageService
    {
        public PageService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> CreatePageAsync(AddPageModel model, int userId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                if (currentUserId <= 0 || currentUserId != userId)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .Select(w => new { w.Id, w.OwnerId })
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId, ct);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == model.WorkspaceId && m.UserId == currentUserId, ct);

                if (workspace.OwnerId != currentUserId && !isMember)
                    return ApiResult.Fail("Bạn không có quyền tạo trang trong workspace này", "FORBIDDEN");

                var page = new Page(model.Title, model.WorkspaceId, currentUserId);

                _dataContext.Set<Page>().Add(page);
                await _dataContext.SaveChangesAsync(ct);

                return ApiResult.Success(page, "Tạo trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tạo trang", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> UpdatePageAsync(UpdatePageModel model, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == model.PageId, ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var workspaceOwnerId = await _dataContext.Set<Workspace>()
                    .Where(w => w.Id == page.WorkspaceId)
                    .Select(w => w.OwnerId)
                    .FirstOrDefaultAsync(ct);

                if (workspaceOwnerId != currentUserId && page.CreatedBy != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền cập nhật trang này", "FORBIDDEN");

                page.UpdateTitle(model.Title, currentUserId);

                await _dataContext.SaveChangesAsync(ct);
                return ApiResult.Success(page, "Cập nhật trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật trang", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> DeletePageAsync(int pageId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == pageId, ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var workspaceOwnerId = await _dataContext.Set<Workspace>()
                    .Where(w => w.Id == page.WorkspaceId)
                    .Select(w => w.OwnerId)
                    .FirstOrDefaultAsync(ct);

                if (workspaceOwnerId != currentUserId && page.CreatedBy != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền xóa trang này", "FORBIDDEN");

                page.Archive(currentUserId);

                await _dataContext.SaveChangesAsync(ct);

                return ApiResult.Success(null, "Xóa trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi xóa trang", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetPageByIdAsync(int pageId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                var page = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.Id == pageId && !p.IsArchived)
                    .FirstOrDefaultAsync(ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                bool hasAccess = await HasAccessToWorkspace(page.WorkspaceId, currentUserId, ct);
                if (!hasAccess)
                    return ApiResult.Fail("Bạn không có quyền xem trang này", "FORBIDDEN");

                return ApiResult.Success(page);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                if (!await HasAccessToWorkspace(workspaceId, currentUserId, ct))
                    return ApiResult.Fail("Không có quyền truy cập", "FORBIDDEN");

                paging ??= new PagingRequest();

                var query = _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.WorkspaceId == workspaceId && !p.IsArchived);

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync(ct);

                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi lấy danh sách trang", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = _userService.UserId;
                if (!await HasAccessToWorkspace(workspaceId, currentUserId, ct))
                    return ApiResult.Fail("Không có quyền truy cập", "FORBIDDEN");

                var query = _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.WorkspaceId == workspaceId && !p.IsArchived);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(p => p.Title.ToLower().Contains(kw));
                }

                paging ??= new PagingRequest();
                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(p => p.Id)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync(ct);

                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi tìm kiếm", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        private async Task<bool> HasAccessToWorkspace(int workspaceId, int userId, CancellationToken ct)
        {
            var workspace = await _dataContext.Set<Workspace>()
                .AsNoTracking()
                .Select(w => new { w.Id, w.OwnerId })
                .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

            if (workspace == null) return false;
            if (workspace.OwnerId == userId) return true;

            return await _dataContext.Set<WorkspaceMember>()
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
        }
    }
}