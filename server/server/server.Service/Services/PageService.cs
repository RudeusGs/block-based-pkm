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
        public PageService(DataContext dataContext, IUserService userService) : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> CreatePageAsync(AddPageModel model, int userId)
        {
            try
            {
                if (model == null)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                if (model.WorkspaceId <= 0 || string.IsNullOrWhiteSpace(model.Title))
                    return ApiResult.Fail("Workspace hoặc title không hợp lệ", "VALIDATION_ERROR");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0 || currentUserId != userId)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == model.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền tạo trang trong workspace này", "FORBIDDEN");

                var page = new Page
                {
                    WorkspaceId = model.WorkspaceId,
                    Title = model.Title.Trim(),
                    CreatedBy = currentUserId,
                    CreatedDate = Now
                };

                _dataContext.Set<Page>().Add(page);
                await SaveChangesAsync();

                return ApiResult.Success(page, "Tạo trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tạo trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> DeletePageAsync(int pageId)
        {
            try
            {
                if (pageId <= 0)
                    return ApiResult.Fail("PageId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == pageId);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == page.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId && page.CreatedBy != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền xóa trang này", "FORBIDDEN");

                _dataContext.Set<Page>().Remove(page);
                await SaveChangesAsync();

                return ApiResult.Success(null, "Xóa trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi xóa trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetPageByIdAsync(int pageId)
        {
            try
            {
                if (pageId <= 0)
                    return ApiResult.Fail("PageId không hợp lệ", "INVALID_INPUT");

                var page = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pageId);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == page.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == page.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem trang này", "FORBIDDEN");

                return ApiResult.Success(page, "Lấy trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem trang trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.WorkspaceId == workspaceId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(p => p.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }
        public async Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền tìm kiếm trang trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<Page>().AsNoTracking().Where(p => p.WorkspaceId == workspaceId);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLowerInvariant();
                    q = q.Where(p => p.Title.ToLower().Contains(kw));
                }

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(p => p.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Tìm kiếm trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tìm kiếm trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdatePageAsync(UpdatePageModel model)
        {
            try
            {
                if (model == null || model.PageId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == model.PageId);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == page.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                // Only owner or page creator can update
                if (workspace.OwnerId != currentUserId && page.CreatedBy != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền cập nhật trang này", "FORBIDDEN");

                if (!string.IsNullOrWhiteSpace(model.Title))
                    page.Title = model.Title.Trim();

                page.MarkUpdated();
                _dataContext.Set<Page>().Update(page);
                await SaveChangesAsync();

                return ApiResult.Success(page, "Cập nhật trang thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật trang", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }
    }
}
