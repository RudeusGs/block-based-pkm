using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.Workspace;

namespace server.Service.Services
{
    public class WorkspaceService : BaseService, IWorkspaceService
    {
        public WorkspaceService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> CreateWorkspaceAsync(AddWorkspaceModel model)
        {
            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Name))
                {
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                }

                var ownerId = _userService.UserId;
                if (ownerId <= 0)
                {
                    return ApiResult.Fail("Không xác định được người dùng", "UNAUTHORIZED");
                }

                var workspace = new Workspace
                {
                    Name = model.Name.Trim(),
                    Description = model.Description?.Trim(),
                    OwnerId = ownerId,
                    CreatedDate = Now
                };

                _dataContext.Set<Workspace>().Add(workspace);
                await SaveChangesAsync();

                var member = new WorkspaceMember
                {
                    WorkspaceId = workspace.Id,
                    UserId = ownerId,
                    Role = RoomRole.GroupLeader,
                    JoinedAt = DateTime.UtcNow
                };

                _dataContext.Set<WorkspaceMember>().Add(member);
                await SaveChangesAsync();

                await transaction.CommitAsync();

                return ApiResult.Success(workspace, "Tạo workspace thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return ApiResult.Fail(
                    "Có lỗi xảy ra khi tạo workspace",
                    "SERVER_ERROR",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResult> DeleteWorkspaceAsync(int workspaceId)
        {
            using var transaction = await _dataContext.Database.BeginTransactionAsync();

            try
            {
                var userId = _userService.UserId;
                if (userId <= 0)
                {
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                }

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == workspaceId);

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                }

                if (workspace.OwnerId != userId)
                {
                    return ApiResult.Fail("Bạn không có quyền xoá workspace này", "FORBIDDEN");
                }

                var members = _dataContext.Set<WorkspaceMember>()
                    .Where(x => x.WorkspaceId == workspaceId);

                _dataContext.RemoveRange(members);

                _dataContext.Set<Workspace>().Remove(workspace);

                await SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResult.Success(null, "Xoá workspace thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return ApiResult.Fail(
                    "Lỗi khi xoá workspace",
                    "SERVER_ERROR",
                    new[] { ex.ToString() }
                );
            }
        }

        public async Task<ApiResult> GetAllByUserIdAsync(int userId, PagingRequest paging)
        {
            try
            {
                if (userId <= 0)
                {
                    return ApiResult.Fail("UserId không hợp lệ", "INVALID_INPUT");
                }

                var pageNumber = paging?.PageNumber > 0 ? paging.PageNumber : 1;
                var pageSize = paging?.PageSize > 0 ? Math.Min(paging.PageSize, 100) : 10;

                var query = _dataContext.Set<WorkspaceMember>()
                    .Where(x => x.UserId == userId)
                    .Join(
                        _dataContext.Set<Workspace>(),
                        wm => wm.WorkspaceId,
                        w => w.Id,
                        (wm, w) => w
                    )
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedDate);

                var total = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return ApiResult.Success(new
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }, "Lấy danh sách workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Lỗi khi lấy danh sách workspace",
                    "SERVER_ERROR",
                    new[] { ex.ToString() }
                );
            }
        }

        public async Task<ApiResult> GetWorkspaceByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResult.Fail("Id không hợp lệ", "INVALID_INPUT");
                }

                var userId = _userService.UserId;

                var workspace = await _dataContext.Set<WorkspaceMember>()
                    .Where(x => x.UserId == userId && x.WorkspaceId == id)
                    .Join(
                        _dataContext.Set<Workspace>(),
                        wm => wm.WorkspaceId,
                        w => w.Id,
                        (wm, w) => w
                    )
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại hoặc không có quyền truy cập", "NOT_FOUND");
                }

                return ApiResult.Success(workspace, "Lấy workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Lỗi khi lấy workspace",
                    "SERVER_ERROR",
                    new[] { ex.ToString() }
                );
            }
        }

        public async Task<ApiResult> UpdateWorkspaceAsync(UpdateWorkspaceModel model)
        {
            try
            {
                if (model == null || model.Id <= 0)
                {
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                }

                var userId = _userService.UserId;
                if (userId <= 0)
                {
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                }

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                }

                if (workspace.OwnerId != userId)
                {
                    return ApiResult.Fail("Bạn không có quyền chỉnh sửa workspace này", "FORBIDDEN");
                }

                if (!string.IsNullOrWhiteSpace(model.Name))
                {
                    workspace.Name = model.Name.Trim();
                }

                if (model.Description != null)
                {
                    workspace.Description = model.Description.Trim();
                }

                workspace.MarkUpdated();

                await SaveChangesAsync();

                return ApiResult.Success(workspace, "Cập nhật workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Lỗi khi cập nhật workspace",
                    "SERVER_ERROR",
                    new[] { ex.ToString() }
                );
            }
        }
    }
}