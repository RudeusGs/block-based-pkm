using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Models;
using server.Service.Models.Workspace;

namespace server.Service.Services
{
    public class WorkspaceService : BaseService, IWorkspaceService
    {
        public WorkspaceService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService) { }

        public async Task<ApiResult> CreateWorkspaceAsync(AddWorkspaceModel model, CancellationToken ct = default)
        {
            using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);
            try
            {
                var ownerId = _userService.UserId;
                if (ownerId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = new Workspace(model.Name?.Trim()!, ownerId, model.Description?.Trim());
                _dataContext.Set<Workspace>().Add(workspace);
                await SaveChangesAsync(ct);

                var member = WorkspaceMember.CreateAsOwner(workspace.Id, ownerId);
                _dataContext.Set<WorkspaceMember>().Add(member);
                await SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
                return ApiResult.Success(workspace, "Tạo workspace thành công");
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
                await transaction.RollbackAsync(ct);
                return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> UpdateWorkspaceAsync(UpdateWorkspaceModel model, CancellationToken ct = default)
        {
            try
            {
                var userId = _userService.UserId;
                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(x => x.Id == model.Id, ct);

                if (workspace == null) return ApiResult.Fail("Không tìm thấy Workspace", "NOT_FOUND");
                if (workspace.OwnerId != userId) return ApiResult.Fail("Forbidden", "FORBIDDEN");

                workspace.UpdateInformation(model.Name!, model.Description);
                await SaveChangesAsync(ct);

                return ApiResult.Success(workspace, "Cập nhật thành công");
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

        public async Task<ApiResult> DeleteWorkspaceAsync(int workspaceId, CancellationToken ct = default)
        {
            using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);
            try
            {
                var userId = _userService.UserId;
                var workspace = await _dataContext.Set<Workspace>().FindAsync(new object[] { workspaceId }, ct);

                if (workspace == null) return ApiResult.Fail("Không tìm thấy Workspace", "NOT_FOUND");
                if (workspace.OwnerId != userId) return ApiResult.Fail("Forbidden", "FORBIDDEN");

                var members = _dataContext.Set<WorkspaceMember>().Where(x => x.WorkspaceId == workspaceId);
                _dataContext.RemoveRange(members);
                _dataContext.Set<Workspace>().Remove(workspace);

                await SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return ApiResult.Success(null, "Đã xóa hoàn toàn Workspace");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return ApiResult.Fail("Lỗi khi xóa", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetAllByUserIdAsync(int userId, PagingRequest paging, CancellationToken ct = default)
        {
            try
            {
                var pageNumber = paging?.PageNumber > 0 ? paging.PageNumber : 1;
                var pageSize = paging?.PageSize > 0 ? Math.Min(paging.PageSize, 100) : 10;

                var query = _dataContext.Set<WorkspaceMember>()
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Workspace)
                    .AsNoTracking()
                    .OrderByDescending(x => x!.CreatedDate);

                var total = await query.CountAsync(ct);
                var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

                return ApiResult.Success(new
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }, "Lấy danh sách thành công");
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

        public async Task<ApiResult> GetWorkspaceByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var userId = _userService.UserId;
                var workspace = await _dataContext.Set<WorkspaceMember>()
                    .Where(x => x.UserId == userId && x.WorkspaceId == id)
                    .Select(x => x.Workspace)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

                return workspace == null
                    ? ApiResult.Fail("Không tìm thấy hoặc không có quyền", "NOT_FOUND")
                    : ApiResult.Success(workspace);
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