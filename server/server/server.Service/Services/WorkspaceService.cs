using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
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
            try
            {
                if (model == null)
                {
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return ApiResult.Fail("Tên workspace không được để trống", "VALIDATION_ERROR");
                }

                var ownerId = _userService.UserId;

                if (ownerId <= 0)
                {
                    return ApiResult.Fail("Không xác định được người dùng", "USER_INVALID");
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

                return ApiResult.Success(workspace, "Tạo workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Có lỗi xảy ra khi tạo workspace",
                    "SERVER_ERROR",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResult> DeleteWorkspaceAsync(int workspaceId)
        {
            try
            {
                var ownerId = _userService.UserId;

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == workspaceId);

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                }

                if (workspace.OwnerId != ownerId)
                {
                    return ApiResult.Fail("Bạn không có quyền xoá workspace này", "FORBIDDEN");
                }

                _dataContext.Set<Workspace>().Remove(workspace);

                await SaveChangesAsync();

                return ApiResult.Success(null, "Xoá workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi xoá workspace", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetAllByUserIdAsync(int userId)
        {
            try
            {
                var workspaces = await _dataContext.Set<Workspace>()
                    .Where(x => x.OwnerId == userId)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return ApiResult.Success(workspaces, "Lấy danh sách workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi lấy danh sách workspace", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetWorkspaceByIdAsync(int id)
        {
            try
            {
                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                }

                return ApiResult.Success(workspace, "Lấy workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi lấy workspace", "SERVER_ERROR", new[] { ex.Message });
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

                var ownerId = _userService.UserId;

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (workspace == null)
                {
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                }

                if (workspace.OwnerId != ownerId)
                {
                    return ApiResult.Fail("Bạn không có quyền chỉnh sửa workspace này", "FORBIDDEN");
                }

                if (!string.IsNullOrWhiteSpace(model.Name))
                {
                    workspace.Name = model.Name.Trim();
                }

                workspace.Description = model.Description?.Trim();
                workspace.MarkUpdated();

                await SaveChangesAsync();

                return ApiResult.Success(workspace, "Cập nhật workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi cập nhật workspace", "SERVER_ERROR", new[] { ex.Message });
            }
        }
    }
}