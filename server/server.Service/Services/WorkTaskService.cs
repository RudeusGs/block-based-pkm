using server.Infrastructure.Realtime.Interfaces;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkTask;
using server.Service.Common.Helpers;

namespace server.Service.Services
{
    public class WorkTaskService : BaseService, IWorkTaskService
    {
        private readonly IRealtimeNotifier _notifier;

        public WorkTaskService(
            DataContext dataContext,
            IUserService userService,
            IRealtimeNotifier notifier
        ) : base(dataContext, userService)
        {
            _notifier = notifier;
        }

        public async Task<ApiResult> UpdateTaskStatusAsync(int taskId, StatusWorkTask newStatus, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0) return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");
                var currentUserId = _userService.UserId;
                if (currentUserId <= 0) return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
                var task = await _dataContext.Set<WorkTask>().FirstOrDefaultAsync(t => t.Id == taskId, ct);
                if (task == null) return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                if (newStatus == StatusWorkTask.Done) task.Complete();
                else if (newStatus == StatusWorkTask.ToDo) task.ReOpen();
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(_notifier, task.WorkspaceId, "TaskStatusUpdated", new { TaskId = task.Id, Status = newStatus });
                return ApiResult.Success(task, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật trạng thái", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByPageAsync(int pageId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (pageId <= 0) return ApiResult.Fail("PageId không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var query = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.PageId == pageId).OrderByDescending(t => t.Id);
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách page tasks", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetUpcomingTasksAsync(int workspaceId, int days = 7, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0) return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var toDate = Now.AddDays(days);
                var query = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId && t.DueDate.HasValue && t.DueDate.Value >= Now && t.DueDate.Value <= toDate).OrderBy(t => t.DueDate);
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy upcoming tasks", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByCreatorAsync(int workspaceId, int userId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0 || userId <= 0) return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var query = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId && t.CreatedById == userId).OrderByDescending(t => t.Id);
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy tasks by creator", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssignedTasksAsync(int workspaceId, int userId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0 || userId <= 0) return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var query = from t in _dataContext.Set<WorkTask>()
                            join a in _dataContext.Set<TaskAssignee>()
                                on t.Id equals a.TaskId
                            where a.UserId == userId && t.WorkspaceId == workspaceId
                            select t;
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy assigned tasks", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksSortedByPriorityAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0) return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var query = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId).OrderByDescending(t => t.Priority);
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách theo priority", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksSortedByDueDateAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0) return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                paging ??= new PagingRequest();
                var query = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId).OrderBy(t => t.DueDate);
                var total = await query.CountAsync(ct);
                var items = await query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
                return ApiResult.Success(new { Total = total, Items = items });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách theo due date", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Title) || model.WorkspaceId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId, ct);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == model.WorkspaceId && m.UserId == userId, ct);
                if (!isMember && workspace.OwnerId != userId)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");
                var priority = model.Priority ?? PriorityWorkTask.Medium;
                var task = new WorkTask(model.Title.Trim(), model.WorkspaceId, userId, model.PageId, priority);
                task.UpdateDetails(task.Title, model.Description, priority, model.DueDate);
                _dataContext.Set<WorkTask>().Add(task);
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskCreated",
                    new { TaskId = task.Id }
                );
                return ApiResult.Success(task, "Tạo task thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tạo task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdateTaskAsync(UpdateWorkTaskModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || model.TaskId <= 0 || string.IsNullOrWhiteSpace(model.Title))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId, ct);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId, ct);
                if (!isMember && workspace.OwnerId != userId)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");
                var priority = model.Priority ?? task.Priority;
                task.UpdateDetails(model.Title.Trim(), model.Description, priority, model.DueDate);
                if (model.Status.HasValue)
                {
                    switch (model.Status.Value)
                    {
                        case StatusWorkTask.Done:
                            task.Complete();
                            break;
                        case StatusWorkTask.ToDo:
                            task.ReOpen();
                            break;
                    }
                }
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskUpdated",
                    new { TaskId = task.Id }
                );
                return ApiResult.Success(task, "Cập nhật task thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> DeleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                if (workspace.OwnerId != userId)
                    return ApiResult.Fail("Chỉ owner được xóa", "FORBIDDEN");
                _dataContext.Set<WorkTask>().Remove(task);
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskDeleted",
                    new { TaskId = task.Id }
                );
                return ApiResult.Success(null, "Xóa task thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi xóa task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> CompleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");
                ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                task.Complete();
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskCompleted",
                    new { TaskId = task.Id }
                );
                return ApiResult.Success(task, "Hoàn thành task");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi complete task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> ReOpenTaskAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");
                ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
                task.ReOpen();
                await SaveChangesAsync(ct);
                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "ReOpenTask",
                    new { TaskId = task.Id }
                );
                return ApiResult.Success(task, "Mở lại task");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "UNAUTHORIZED");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi reopen", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetRecommendedTasksAsync(int workspaceId, int limit = 5, CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                var tasks = await _dataContext.Set<WorkTask>()
                    .AsNoTracking()
                    .Where(t => t.WorkspaceId == workspaceId && t.Status != StatusWorkTask.Done)
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.DueDate)
                    .Take(limit)
                    .ToListAsync(ct);
                return ApiResult.Success(tasks, "OK");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTaskByIdAsync(int taskId, CancellationToken ct = default)
        {
            var task = await _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task == null)
                return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");
            return ApiResult.Success(task, "OK");
        }

        public async Task<ApiResult> GetTasksByWorkspaceAsync(
            int workspaceId,
            PagingRequest? paging = null,
            CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                IQueryable<WorkTask> query = _dataContext.Set<WorkTask>()
                    .AsNoTracking()
                    .Where(t => t.WorkspaceId == workspaceId);
                query = query.OrderByDescending(t => t.Id); // Đã sửa để phù hợp với entity (không còn RecommendationWeight)
                if (paging != null)
                {
                    var skip = (paging.PageNumber - 1) * paging.PageSize;
                    query = query.Skip(skip)
                                 .Take(paging.PageSize);
                }
                var items = await query.ToListAsync(ct);
                return ApiResult.Success(items, "OK");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy tasks", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByStatusAsync(int workspaceId, StatusWorkTask status, PagingRequest? paging = null, CancellationToken ct = default)
        {
            var query = _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId && t.Status == status);
            if (paging != null)
                query = query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize);
            var items = await query.ToListAsync(ct);
            return ApiResult.Success(items, "OK");
        }

        public async Task<ApiResult> GetOverdueTasksAsync(
            int workspaceId,
            PagingRequest? paging = null,
            CancellationToken ct = default)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");
                IQueryable<WorkTask> query = _dataContext.Set<WorkTask>()
                    .AsNoTracking()
                    .Where(t => t.WorkspaceId == workspaceId &&
                                t.DueDate.HasValue &&
                                t.DueDate < DateTime.UtcNow &&
                                t.Status != StatusWorkTask.Done);
                query = query.OrderBy(t => t.DueDate);
                if (paging != null)
                {
                    var skip = (paging.PageNumber - 1) * paging.PageSize;
                    query = query.Skip(skip)
                                 .Take(paging.PageSize);
                }
                var items = await query.ToListAsync(ct);
                return ApiResult.Success(items, "OK");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy overdue tasks", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> SearchTasksAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default)
        {
            var query = _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t =>
                    EF.Functions.Like(t.Title, $"%{keyword}%") ||
                    (t.Description != null && EF.Functions.Like(t.Description, $"%{keyword}%")));
            }
            if (paging != null)
                query = query.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize);
            var items = await query.ToListAsync(ct);
            return ApiResult.Success(items, "OK");
        }

        public async Task<ApiResult> GetTaskStatisticsAsync(int workspaceId, CancellationToken ct = default)
        {
            var tasks = await _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId)
                .ToListAsync(ct);

            var total = tasks.Count;
            var completed = tasks.Count(t => t.Status == StatusWorkTask.Done);
            var overdue = tasks.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && t.Status != StatusWorkTask.Done);

            return ApiResult.Success(new
            {
                Total = total,
                Completed = completed,
                Overdue = overdue
            }, "OK");
        }
    }
}