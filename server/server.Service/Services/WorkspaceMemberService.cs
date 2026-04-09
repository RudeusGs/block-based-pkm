using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkspaceMember;

namespace server.Service.Services
{
    public class WorkspaceMemberService : BaseService, IWorkspaceMemberService
    {
        private readonly IPermissionService _permissionService;

        public WorkspaceMemberService(
            DataContext dataContext, 
            IUserService userService,
            IPermissionService permissionService
        ) : base(dataContext, userService)
        {
            _permissionService = permissionService;
        }

        public async Task<ApiResult> AddMemberAsync(AddWorkspaceMemberModel model, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(currentUserId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.IsWorkspaceOwnerAsync(model.WorkspaceId, currentUserId, ct))
                    return ApiResult.Fail("Bạn không có quyền thêm thành viên hoặc Workspace không tồn tại", "FORBIDDEN");

                var user = await _dataContext.Set<User>()
                    .FirstOrDefaultAsync(x => x.Email == model.UserEmail.Trim(), ct);
                if (user == null) return ApiResult.Fail("User không tồn tại", "NOT_FOUND");

                if (user.Id == currentUserId) return ApiResult.Fail("Bạn đã là thành viên", "VALIDATION_ERROR");
                var exists = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(x => x.WorkspaceId == model.WorkspaceId && x.UserId == user.Id, ct);
                if (exists) return ApiResult.Fail("User đã tồn tại trong workspace", "DUPLICATE");

                var member = new WorkspaceMember(model.WorkspaceId, user.Id);
                _dataContext.Set<WorkspaceMember>().Add(member);
                await SaveChangesAsync(ct);
                return ApiResult.Success(null, "Thêm thành viên thành công");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetWorkspaceMembersAsync(int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(currentUserId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.HasWorkspaceAccessAsync(workspaceId, currentUserId, ct))
                    return ApiResult.Fail("Bạn không có quyền xem danh sách này", "FORBIDDEN");

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
                                     }).AsNoTracking().ToListAsync(ct);

                return ApiResult.Success(members, "OK");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> RemoveMemberAsync(int workspaceId, int userId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(currentUserId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                var isOwner = await _permissionService.IsWorkspaceOwnerAsync(workspaceId, currentUserId, ct);
                if (!isOwner && currentUserId != userId)
                    return ApiResult.Fail("Bạn không có quyền thực hiện hành động này", "FORBIDDEN");

                if (await _permissionService.IsWorkspaceOwnerAsync(workspaceId, userId, ct))
                    return ApiResult.Fail("Không thể xóa chủ sở hữu", "FORBIDDEN");

                var member = await _dataContext.Set<WorkspaceMember>()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

                if (member == null) return ApiResult.Fail("Thành viên không tồn tại trong workspace", "NOT_FOUND");

                _dataContext.Set<WorkspaceMember>().Remove(member);
                await SaveChangesAsync(ct);

                return ApiResult.Success(null, "Xóa thành viên thành công");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> UpdateMemberRoleAsync(int workspaceId, int userId, RoomRole role, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                if(currentUserId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (!await _permissionService.IsWorkspaceOwnerAsync(workspaceId, currentUserId, ct))
                    return ApiResult.Fail("Chỉ chủ sở hữu mới có thể thay đổi vai trò hoặc Workspace không tồn tại", "FORBIDDEN");

                var member = await _dataContext.Set<WorkspaceMember>()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

                if (member == null) return ApiResult.Fail("Thành viên không tồn tại", "NOT_FOUND");
                member.AssignRole(role);

                await SaveChangesAsync(ct);

                return ApiResult.Success(null, "Cập nhật vai trò thành công");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }
    }
}