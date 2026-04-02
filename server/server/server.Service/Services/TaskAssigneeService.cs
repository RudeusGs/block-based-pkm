using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using server.Infrastructure.Realtime.Hubs;

namespace server.Service.Services
{
    public class TaskAssigneeService : BaseService, ITaskAssigneeService
    {
        private readonly IHubContext<WorkspaceHub> _hubContext;

        public TaskAssigneeService(DataContext dataContext, IUserService userService,
            IHubContext<WorkspaceHub> hubContext) : base(dataContext, userService)
        {
            _hubContext = hubContext;
        }

        public async Task<ApiResult> AssignTaskAsync(int taskId, int userId)
        {
            try
            {
                if (taskId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner của workspace mới có quyền phân công", "FORBIDDEN");
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId);
                if (!isMember)
                    return ApiResult.Fail("Người được phân công không thuộc workspace", "VALIDATION_ERROR");
                var exists = await _dataContext.Set<TaskAssignee>().AnyAsync(a => a.TaskId == taskId && a.UserId == userId);
                if (exists)
                    return ApiResult.Fail("User đã được phân công cho task này", "ALREADY_ASSIGNED");

                var assignee = new TaskAssignee
                {
                    TaskId = taskId,
                    UserId = userId,
                    AssignedAt = Now,
                    CreatedDate = Now
                };

                _dataContext.Set<TaskAssignee>().Add(assignee);
                await SaveChangesAsync();
                try
                {
                    await _hubContext.Clients.Group(WorkspaceHub.GetWorkspaceGroupName(workspace.Id))
                        .SendAsync("TaskAssigneeAdded", new { TaskId = taskId, UserId = userId });
                }
                catch(Exception ex) 
                {
                    return ApiResult.Success(assignee, "Gán task thành công, nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(assignee, "Gán task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi phân công task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> AssignTaskToMultipleUsersAsync(int taskId, List<int> userIds)
        {
            try
            {
                if (taskId <= 0 || userIds == null || userIds.Count == 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner của workspace mới có quyền phân công", "FORBIDDEN");

                var members = await _dataContext.Set<WorkspaceMember>().AsNoTracking()
                    .Where(m => m.WorkspaceId == workspace.Id && userIds.Contains(m.UserId))
                    .Select(m => m.UserId)
                    .ToListAsync();

                var added = new List<TaskAssignee>();
                foreach (var uid in members)
                {
                    var exists = await _dataContext.Set<TaskAssignee>().AnyAsync(a => a.TaskId == taskId && a.UserId == uid);
                    if (exists) continue;

                    var assignee = new TaskAssignee
                    {
                        TaskId = taskId,
                        UserId = uid,
                        AssignedAt = Now,
                        CreatedDate = Now
                    };
                    _dataContext.Set<TaskAssignee>().Add(assignee);
                    added.Add(assignee);
                }

                await SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.Group(WorkspaceHub.GetWorkspaceGroupName(workspace.Id))
                        .SendAsync("TaskAssigneesAdded", new { TaskId = taskId, UserIds = added.Select(a => a.UserId).ToList() });
                }
                catch (Exception ex)
                {
                    return ApiResult.Success(new { Added = added.Count, Items = added }, "Gán nhiều user thành công, nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(new { Added = added.Count, Items = added }, "Gán nhiều user thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi phân công nhiều user", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssignedTasksAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                    return ApiResult.Fail("UserId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                if (currentUserId != userId)
                {
                    var ownerOfAny = await _dataContext.Set<Workspace>().AsNoTracking().AnyAsync(w => w.OwnerId == currentUserId);
                    if (!ownerOfAny)
                        return ApiResult.Fail("Không có quyền xem danh sách này", "FORBIDDEN");
                }

                var assignedTaskIds = await _dataContext.Set<TaskAssignee>().AsNoTracking().Where(a => a.UserId == userId).Select(a => a.TaskId).ToListAsync();
                var tasks = await _dataContext.Set<WorkTask>().AsNoTracking().Where(t => assignedTaskIds.Contains(t.Id)).ToListAsync();

                return ApiResult.Success(tasks, "Lấy danh sách task đã được giao thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task được giao", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssignedTasksByUsersAsync(List<int> userIds)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var assignees = await _dataContext.Set<TaskAssignee>().AsNoTracking()
                    .Where(a => userIds.Contains(a.UserId))
                    .ToListAsync();

                return ApiResult.Success(assignees, "Lấy danh sách task theo users thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssignedTasksByWorkspaceAsync(int userId, int workspaceId)
        {
            try
            {
                if (userId <= 0 || workspaceId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == workspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);
                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem danh sách này", "FORBIDDEN");

                var taskIds = await _dataContext.Set<TaskAssignee>().AsNoTracking().Where(a => a.UserId == userId).Select(a => a.TaskId).ToListAsync();
                var tasks = await _dataContext.Set<WorkTask>().AsNoTracking().Where(t => taskIds.Contains(t.Id) && t.WorkspaceId == workspaceId).ToListAsync();

                return ApiResult.Success(tasks, "Lấy danh sách task theo workspace thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssigneesAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId);
                var isAssignee = await _dataContext.Set<TaskAssignee>().AsNoTracking().AnyAsync(a => a.TaskId == taskId && a.UserId == currentUserId);
                if (!isOwner && !isMember && !isAssignee)
                    return ApiResult.Fail("Bạn không có quyền xem danh sách này", "FORBIDDEN");

                var assignees = await _dataContext.Set<TaskAssignee>().AsNoTracking().Where(a => a.TaskId == taskId).ToListAsync();
                return ApiResult.Success(assignees, "Lấy danh sách người được phân công thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> IsAssignedAsync(int taskId, int userId)
        {
            try
            {
                if (taskId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                if (currentUserId != userId)
                {
                    var ownerOfAny = await _dataContext.Set<Workspace>().AsNoTracking().AnyAsync(w => w.OwnerId == currentUserId);
                    if (!ownerOfAny)
                        return ApiResult.Fail("Bạn không có quyền kiểm tra", "FORBIDDEN");
                }

                var exists = await _dataContext.Set<TaskAssignee>().AsNoTracking().AnyAsync(a => a.TaskId == taskId && a.UserId == userId);
                return ApiResult.Success(exists, "Kiểm tra phân công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi kiểm tra", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> ReassignTaskAsync(int taskId, int oldUserId, int newUserId)
        {
            try
            {
                if (taskId <= 0 || oldUserId <= 0 || newUserId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner của workspace mới có quyền chuyển giao", "FORBIDDEN");

                var existedOld = await _dataContext.Set<TaskAssignee>().FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == oldUserId);
                if (existedOld == null)
                    return ApiResult.Fail("Old assignee không tồn tại trên task", "NOT_FOUND");

                var isNewMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == newUserId);
                if (!isNewMember)
                    return ApiResult.Fail("New user không thuộc workspace", "VALIDATION_ERROR");

                _dataContext.Set<TaskAssignee>().Remove(existedOld);

                var existsNew = await _dataContext.Set<TaskAssignee>().AnyAsync(a => a.TaskId == taskId && a.UserId == newUserId);
                if (!existsNew)
                {
                    var assignee = new TaskAssignee
                    {
                        TaskId = taskId,
                        UserId = newUserId,
                        AssignedAt = Now,
                        CreatedDate = Now
                    };
                    _dataContext.Set<TaskAssignee>().Add(assignee);
                }

                await SaveChangesAsync();
                return ApiResult.Success(null, "Chuyển giao assignee thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi chuyển giao assignee", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UnassignTaskAsync(int taskId, int userId)
        {
            try
            {
                if (taskId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner của workspace mới có quyền hủy phân công", "FORBIDDEN");

                var existed = await _dataContext.Set<TaskAssignee>().FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId);
                if (existed == null)
                    return ApiResult.Fail("Assignee không tồn tại", "NOT_FOUND");

                _dataContext.Set<TaskAssignee>().Remove(existed);
                await SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.Group(WorkspaceHub.GetWorkspaceGroupName(workspace.Id))
                        .SendAsync("TaskAssigneeRemoved", new { TaskId = taskId, UserId = userId });
                }
                catch (Exception ex)
                {
                    return ApiResult.Success(null, "Hủy phân công thành công, nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(null, "Hủy phân công thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi hủy phân công", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UnassignTaskFromAllAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner của workspace mới có quyền hủy phân công", "FORBIDDEN");

                var list = await _dataContext.Set<TaskAssignee>().Where(a => a.TaskId == taskId).ToListAsync();
                if (list.Count == 0)
                    return ApiResult.Success(null, "Không có assignee nào để xóa");

                _dataContext.Set<TaskAssignee>().RemoveRange(list);
                await SaveChangesAsync();

                try
                {
                    await _hubContext.Clients.Group(WorkspaceHub.GetWorkspaceGroupName(workspace.Id))
                        .SendAsync("TaskAssigneesCleared", new { TaskId = taskId });
                }
                catch (Exception ex)
                {
                    return ApiResult.Success(null, "Hủy phân công tất cả thành công, nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(null, "Hủy phân công tất cả thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi hủy phân công tất cả", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }
    }
}
