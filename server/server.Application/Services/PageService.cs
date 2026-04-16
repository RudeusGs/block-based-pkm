using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.Page;

namespace server.Service.Services
{
    public class PageService : BaseService, IPageService
    {
        private readonly IPermissionService _permissionService;
        private readonly IActivityLogService _activityLogService;

        public PageService(
            DataContext dataContext,
            IUserService userService,
            IPermissionService permissionService,
            IActivityLogService activityLogService
        ) : base(dataContext, userService)
        {
            _permissionService = permissionService;
            _activityLogService = activityLogService;
        }

        public async Task<ApiResult> CreatePageAsync(AddPageModel model, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.HasWorkspaceAccessAsync(model.WorkspaceId, userId, ct))
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                if (model.ParentPageId.HasValue)
                {
                    var parentActive = await _dataContext.Set<Page>()
                        .AnyAsync(p =>
                            p.Id == model.ParentPageId &&
                            p.WorkspaceId == model.WorkspaceId &&
                            !p.IsDeleted &&
                            !p.IsArchived, ct);

                    if (!parentActive)
                        return ApiResult.Fail("Parent không hợp lệ hoặc đã bị vô hiệu hóa", "INVALID_PARENT");
                }

                var page = new Page(model.Title, model.WorkspaceId, userId, model.ParentPageId);

                if (model.Content != null)
                    page.UpdateContent(model.Content, userId);

                if (model.Icon != null || model.CoverImage != null)
                    page.UpdateAppearance(model.Icon, model.CoverImage, userId);

                _dataContext.Set<Page>().Add(page);
                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(model.WorkspaceId, userId, "Create", "Page", page.Id, null, ct);

                return ApiResult.Success(page, "Tạo trang thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi khi tạo trang", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> UpdatePageAsync(int pageId, UpdatePageModel model, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == pageId && !p.IsDeleted && !p.IsArchived, ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var isWorkspaceOwner = await _permissionService.IsWorkspaceOwnerAsync(page.WorkspaceId, userId, ct);

                if (!isWorkspaceOwner && page.CreatedBy != userId)
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                if (!string.IsNullOrWhiteSpace(model.Title))
                    page.UpdateTitle(model.Title, userId);

                if (model.Icon != null || model.CoverImage != null)
                    page.UpdateAppearance(model.Icon, model.CoverImage, userId);

                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(page.WorkspaceId, userId, "Update", "Page", page.Id, null, ct);

                return ApiResult.Success(page, "Cập nhật thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi khi cập nhật", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> DeletePageAsync(int pageId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                var page = await _dataContext.Set<Page>()
                    .FirstOrDefaultAsync(p => p.Id == pageId && !p.IsDeleted && !p.IsArchived, ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var isWorkspaceOwner = await _permissionService.IsWorkspaceOwnerAsync(page.WorkspaceId, userId, ct);

                if (!isWorkspaceOwner && page.CreatedBy != userId)
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                var allDescendantIds = new HashSet<int>();
                var frontier = new Queue<int>(new[] { pageId });

                while (frontier.Count > 0)
                {
                    var currentBatch = new List<int>();
                    while (frontier.Count > 0) currentBatch.Add(frontier.Dequeue());

                    var childIds = await _dataContext.Set<Page>()
                        .Where(p => p.ParentPageId.HasValue &&
                                    currentBatch.Contains(p.ParentPageId.Value) &&
                                    !p.IsDeleted && !p.IsArchived)
                        .Select(p => p.Id)
                        .ToListAsync(ct);

                    foreach (var id in childIds)
                    {
                        if (allDescendantIds.Add(id))
                            frontier.Enqueue(id);
                    }
                }

                if (allDescendantIds.Count > 0)
                {
                    var descendants = await _dataContext.Set<Page>()
                        .Where(p => allDescendantIds.Contains(p.Id))
                        .ToListAsync(ct);

                    foreach (var desc in descendants)
                        desc.Archive(userId);
                }

                page.Archive(userId);

                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(page.WorkspaceId, userId, "Archive", "Page", page.Id, null, ct);

                return ApiResult.Success(new { ArchivedCount = allDescendantIds.Count + 1 }, "Đã archive page và toàn bộ con");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi khi xóa", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetPageByIdAsync(int pageId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var page = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pageId && !p.IsDeleted && !p.IsArchived, ct);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                if (!await _permissionService.HasWorkspaceAccessAsync(page.WorkspaceId, userId, ct))
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                return ApiResult.Success(page);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.HasWorkspaceAccessAsync(workspaceId, userId, ct))
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                var query = _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.WorkspaceId == workspaceId &&
                                p.ParentPageId == null &&
                                !p.IsDeleted &&
                                !p.IsArchived)
                    .OrderByDescending(p => p.CreatedDate);

                return await GetPagedAsync(query, paging, ct);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi lấy danh sách", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetSubPagesAsync(int parentPageId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var parent = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == parentPageId && !p.IsDeleted && !p.IsArchived, ct);

                if (parent == null)
                    return ApiResult.Fail("Page không tồn tại hoặc đã bị vô hiệu hóa", "NOT_FOUND");

                if (!await _permissionService.HasWorkspaceAccessAsync(parent.WorkspaceId, userId, ct))
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                var subPages = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.ParentPageId == parentPageId && !p.IsDeleted && !p.IsArchived)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync(ct);

                return ApiResult.Success(subPages);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi lấy sub-pages", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(userId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.HasWorkspaceAccessAsync(workspaceId, userId, ct))
                    return ApiResult.Fail("Forbidden", "FORBIDDEN");

                var query = _dataContext.Set<Page>()
                    .AsNoTracking()
                    .Where(p => p.WorkspaceId == workspaceId && !p.IsDeleted && !p.IsArchived);

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(p => p.Title.Contains(keyword.Trim()));

                var orderedQuery = query.OrderByDescending(p => p.CreatedDate);

                return await GetPagedAsync(orderedQuery, paging, ct);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Lỗi tìm kiếm", "SERVER_ERROR"); }
        }
    }
}