using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkspaceMember;

namespace server.Service.Services
{
    public class WorkspaceMemberService : BaseService, IWorkspaceMemberService
    {
        public WorkspaceMemberService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService) { }

        public async Task<ApiResult> AddMemberAsync(AddWorkspaceMemberModel model)
        {
            try
            {
                if (model == null || model.WorkspaceId <= 0 || string.IsNullOrWhiteSpace(model.UserEmail))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == model.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền thêm thành viên", "FORBIDDEN");

                var user = await _dataContext.Set<User>()
                    .FirstOrDefaultAsync(x => x.Email == model.UserEmail.Trim());

                if (user == null)
                    return ApiResult.Fail("User không tồn tại", "NOT_FOUND");

                if (user.Id == currentUserId)
                    return ApiResult.Fail("Bạn đã là thành viên", "VALIDATION_ERROR");

                var exists = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(x => x.WorkspaceId == model.WorkspaceId && x.UserId == user.Id);

                if (exists)
                    return ApiResult.Fail("User đã tồn tại trong workspace", "DUPLICATE");

                var member = new WorkspaceMember
                {
                    WorkspaceId = model.WorkspaceId,
                    UserId = user.Id,
                };

                _dataContext.Set<WorkspaceMember>().Add(member);
                await SaveChangesAsync();

                return ApiResult.Success(null, "Thêm thành viên thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Có lỗi xảy ra khi thêm thành viên",
                    "SERVER_ERROR",
                    new[] { ex.Message }
                );
            }
        }

        public async Task<ApiResult> GetWorkspaceMembersAsync(int workspaceId)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var hasAccess = await _dataContext.Set<Workspace>()
                    .AnyAsync(w => w.Id == workspaceId &&
                                  (w.OwnerId == currentUserId ||
                                   _dataContext.Set<WorkspaceMember>()
                                       .Any(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId)));

                if (!hasAccess)
                    return ApiResult.Fail("Bạn không có quyền xem", "FORBIDDEN");

                var members = await (from m in _dataContext.Set<WorkspaceMember>()
                                     join u in _dataContext.Set<User>() on m.UserId equals u.Id
                                     where m.WorkspaceId == workspaceId
                                     select new
                                     {
                                         m.Id,
                                         m.UserId,
                                         u.UserName,
                                         u.Email,
                                         u.FullName,
                                         m.Role,
                                         m.JoinedAt
                                     }).ToListAsync();

                return ApiResult.Success(members, "OK");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Có lỗi khi lấy danh sách thành viên",
                    "SERVER_ERROR",
                    new[] { ex.Message }
                );
            }
        }

        public async Task<ApiResult> RemoveMemberAsync(int workspaceId, int userId)
        {
            try
            {
                if (workspaceId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (userId == workspace.OwnerId)
                    return ApiResult.Fail("Không thể xóa owner", "FORBIDDEN");

                if (currentUserId != workspace.OwnerId && currentUserId != userId)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var member = await _dataContext.Set<WorkspaceMember>()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if (member == null)
                    return ApiResult.Fail("Member không tồn tại", "NOT_FOUND");

                _dataContext.Set<WorkspaceMember>().Remove(member);
                await SaveChangesAsync();

                return ApiResult.Success(null, "Xóa thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Lỗi khi xóa thành viên",
                    "SERVER_ERROR",
                    new[] { ex.Message }
                );
            }
        }

        public async Task<ApiResult> UpdateMemberRoleAsync(UpdateWorkspaceMemberModel model)
        {
            try
            {
                if (model == null || model.WorkspaceId <= 0 || model.WorkspaceMemberId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var member = await _dataContext.Set<WorkspaceMember>()
                    .FirstOrDefaultAsync(m => m.Id == model.WorkspaceMemberId);

                if (member == null)
                    return ApiResult.Fail("Member không tồn tại", "NOT_FOUND");

                member.Role = model.Role;
                await SaveChangesAsync();

                return ApiResult.Success(null, "Cập nhật role thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Lỗi khi cập nhật role",
                    "SERVER_ERROR",
                    new[] { ex.Message }
                );
            }
        }
    }
}