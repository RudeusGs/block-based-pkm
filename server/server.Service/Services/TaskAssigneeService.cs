using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Service.Services
{
    public class TaskAssigneeService : BaseService, ITaskAssigneeService
    {
        private readonly IRealtimeNotifier _notifier;

        public TaskAssigneeService(
            DataContext dataContext,
            IUserService userService,
            IRealtimeNotifier notifier
        ) : base(dataContext, userService)
        {
            _notifier = notifier;
        }

        public async Task<ApiResult> AssignTaskAsync(int taskId, int userId, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var item = await (from t in _dataContext.Set<WorkTask>()
                                  join w in _dataContext.Set<Workspace>() on t.WorkspaceId equals w.Id
                                  where t.Id == taskId
                                  select new { Task = t, Workspace = w })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

                if (item == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                if (item.Workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner mới được assign", "FORBIDDEN");

                var isMemberTask = _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == item.Workspace.Id && m.UserId == userId, ct);

                var alreadyAssignedTask = _dataContext.Set<TaskAssignee>()
                    .AsNoTracking()
                    .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

                await Task.WhenAll(isMemberTask, alreadyAssignedTask);

                if (!isMemberTask.Result)
                    return ApiResult.Fail("User không thuộc workspace", "VALIDATION_ERROR");

                if (alreadyAssignedTask.Result)
                    return ApiResult.Fail("Đã assign rồi", "ALREADY_ASSIGNED");

                var assignee = new TaskAssignee(taskId, userId);
                _dataContext.Set<TaskAssignee>().Add(assignee);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, item.Workspace.Id, "TaskAssigneeAdded",
                    new { TaskId = taskId, UserId = userId });

                return ApiResult.Success(assignee, "Assign thành công");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi khi assign task", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> AssignTaskToMultipleUsersAsync(int taskId, List<int> userIds, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0 || userIds == null || userIds.Count == 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var item = await (from t in _dataContext.Set<WorkTask>()
                                  join w in _dataContext.Set<Workspace>() on t.WorkspaceId equals w.Id
                                  where t.Id == taskId
                                  select new { Task = t, Workspace = w })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

                if (item == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                if (item.Workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner mới được assign", "FORBIDDEN");

                var validUserIds = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .Where(m => m.WorkspaceId == item.Workspace.Id && userIds.Contains(m.UserId))
                    .Select(m => m.UserId)
                    .ToListAsync(ct);

                if (!validUserIds.Any())
                {
                    return ApiResult.Success(new { Added = 0 }, "Assign nhiều user thành công");
                }

                var existed = await _dataContext.Set<TaskAssignee>()
                    .AsNoTracking()
                    .Where(a => a.TaskId == taskId && validUserIds.Contains(a.UserId))
                    .Select(a => a.UserId)
                    .ToListAsync(ct);

                var toAdd = validUserIds.Except(existed).ToList();

                if (toAdd.Any())
                {
                    var assignees = toAdd.Select(uid => new TaskAssignee(taskId, uid));
                    _dataContext.Set<TaskAssignee>().AddRange(assignees);
                    await SaveChangesAsync(ct);

                    await ServiceHelper.SafeNotifyAsync(_notifier, item.Workspace.Id, "TaskAssigneesAdded",
                        new { TaskId = taskId, UserIds = toAdd });
                }

                return ApiResult.Success(new { Added = toAdd.Count }, "Assign nhiều user thành công");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi khi assign multiple", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> UnassignTaskAsync(int taskId, int userId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var item = await (from t in _dataContext.Set<WorkTask>()
                                  join w in _dataContext.Set<Workspace>() on t.WorkspaceId equals w.Id
                                  where t.Id == taskId
                                  select new { Task = t, Workspace = w })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);

                if (item == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                if (item.Workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner mới được unassign", "FORBIDDEN");

                var entity = await _dataContext.Set<TaskAssignee>()
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

                if (entity == null)
                    return ApiResult.Fail("Không tồn tại assignee", "NOT_FOUND");

                _dataContext.Set<TaskAssignee>().Remove(entity);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, item.Workspace.Id, "TaskAssigneeRemoved",
                    new { TaskId = taskId, UserId = userId });

                return ApiResult.Success(null, "Unassign thành công");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (OperationCanceledException)
            {
                return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED");
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi khi unassign", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> GetAssigneesAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId, ct);

                if (workspace.OwnerId != currentUserId && !isMember)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var data = await _dataContext.Set<TaskAssignee>()
                    .Where(a => a.TaskId == taskId)
                    .ToListAsync(ct);

                return ApiResult.Success(data, "OK");
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> IsAssignedAsync(int taskId, int userId, CancellationToken ct = default)
        {
            try
            {
                ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var exists = await _dataContext.Set<TaskAssignee>()
                    .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

                return ApiResult.Success(exists);
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> UnassignTaskFromAllAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner mới được clear", "FORBIDDEN");

                var list = await _dataContext.Set<TaskAssignee>()
                    .Where(a => a.TaskId == taskId)
                    .ToListAsync(ct);

                _dataContext.Set<TaskAssignee>().RemoveRange(list);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, workspace.Id, "TaskAssigneesCleared",
                    new { TaskId = taskId });

                return ApiResult.Success(null, "Clear thành công");
            }
            catch (Exception)
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> GetAssignedTasksAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                if (userId <= 0)
                    return ApiResult.Fail("UserId không hợp lệ", "INVALID_INPUT");

                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                if (currentUserId != userId)
                {
                    var isOwner = await _dataContext.Set<Workspace>()
                        .AsNoTracking()
                        .AnyAsync(w => w.OwnerId == currentUserId, ct);

                    if (!isOwner)
                        return ApiResult.Fail("Không có quyền xem", "FORBIDDEN");
                }

                var tasks = await _dataContext.Set<TaskAssignee>()
                    .Where(a => a.UserId == userId)
                    .Join(_dataContext.Set<WorkTask>(),
                        a => a.TaskId,
                        t => t.Id,
                        (a, t) => t)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return ApiResult.Success(tasks, "OK");
            }
            catch
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> GetAssignedTasksByWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                if (userId <= 0 || workspaceId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, ct);

                if (workspace.OwnerId != currentUserId && !isMember)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var tasks = await _dataContext.Set<TaskAssignee>()
                    .Where(a => a.UserId == userId)
                    .Join(_dataContext.Set<WorkTask>(),
                        a => a.TaskId,
                        t => t.Id,
                        (a, t) => t)
                    .Where(t => t.WorkspaceId == workspaceId)
                    .AsNoTracking()
                    .ToListAsync(ct);

                return ApiResult.Success(tasks, "OK");
            }
            catch
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> GetAssignedTasksByUsersAsync(List<int> userIds, CancellationToken ct = default)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var tasks = await _dataContext.Set<TaskAssignee>()
                    .Where(a => userIds.Contains(a.UserId))
                    .Join(_dataContext.Set<WorkTask>(),
                        a => a.TaskId,
                        t => t.Id,
                        (a, t) => new { a.UserId, Task = t })
                    .AsNoTracking()
                    .ToListAsync(ct);

                return ApiResult.Success(tasks, "OK");
            }
            catch
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }

        public async Task<ApiResult> ReassignTaskAsync(int taskId, int oldUserId, int newUserId, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0 || oldUserId <= 0 || newUserId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Chỉ owner mới được reassign", "FORBIDDEN");

                var oldAssignee = await _dataContext.Set<TaskAssignee>()
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == oldUserId, ct);

                if (oldAssignee == null)
                    return ApiResult.Fail("Old assignee không tồn tại", "NOT_FOUND");

                var isNewMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == newUserId, ct);

                if (!isNewMember)
                    return ApiResult.Fail("User mới không thuộc workspace", "VALIDATION_ERROR");

                _dataContext.Set<TaskAssignee>().Remove(oldAssignee);

                var exists = await _dataContext.Set<TaskAssignee>()
                    .AnyAsync(a => a.TaskId == taskId && a.UserId == newUserId, ct);

                if (!exists)
                {
                    _dataContext.Set<TaskAssignee>()
                        .Add(new TaskAssignee(taskId, newUserId));
                }

                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, workspace.Id, "TaskReassigned",
                    new { TaskId = taskId, OldUserId = oldUserId, NewUserId = newUserId });

                return ApiResult.Success(null, "Reassign thành công");
            }
            catch
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR");
            }
        }
    }
}