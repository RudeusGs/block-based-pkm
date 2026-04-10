using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Services
{
    public class WorkTaskService : BaseService, IWorkTaskService
    {
        private readonly IRealtimeNotifier _notifier;
        private readonly IPermissionService _permissionService;
        private readonly ITaskPerformanceMetricService _taskPerformanceMetricService;
        private readonly IActivityLogService _activityLogService;

        public WorkTaskService(
            DataContext dataContext,
            IUserService userService,
            IRealtimeNotifier notifier,
            IPermissionService permissionService,
            ITaskPerformanceMetricService taskPerformanceMetricService,
            IActivityLogService activityLogService
        ) : base(dataContext, userService)
        {
            _notifier = notifier;
            _permissionService = permissionService;
            _taskPerformanceMetricService = taskPerformanceMetricService;
            _activityLogService = activityLogService;
        }

        public async Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Title) || model.WorkspaceId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId, ct);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (!await _permissionService.HasWorkspaceAccessAsync(model.WorkspaceId, userId, ct))
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var priority = model.Priority ?? PriorityWorkTask.Medium;

                var task = new WorkTask(
                    model.Title.Trim(),
                    model.WorkspaceId,
                    userId,
                    model.PageId,
                    priority
                );

                if (!string.IsNullOrWhiteSpace(model.Description) || model.DueDate.HasValue)
                {
                    task.UpdateDetails(task.Title, model.Description, priority, model.DueDate);
                }

                _dataContext.Set<WorkTask>().Add(task);
                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(task.WorkspaceId, userId, "Create", "WorkTask", task.Id);

                await _taskPerformanceMetricService.CreateMetricAsync(task.Id, userId, task.WorkspaceId);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskCreated",
                    new { TaskId = task.Id }
                );

                return ApiResult.Success(task, "Tạo task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi tạo task", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> UpdateTaskAsync(int taskId, UpdateWorkTaskModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || taskId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                if (!await _permissionService.HasWorkspaceAccessAsync(task.WorkspaceId, userId, ct))
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                var title = string.IsNullOrWhiteSpace(model.Title)
                    ? task.Title
                    : model.Title.Trim();

                var priority = model.Priority ?? task.Priority;

                task.UpdateDetails(title, model.Description, priority, model.DueDate);

                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(task.WorkspaceId, userId, "Update", "WorkTask", task.Id);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskUpdated",
                    new { TaskId = task.Id }
                );

                if (model.Status.HasValue)
                {
                    var statusResult = await ChangeStatus(taskId, model.Status.Value, ct);
                    if (!statusResult.IsSuccess) return statusResult;
                }

                return ApiResult.Success(task, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi cập nhật task", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> DeleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var isOwner = await _permissionService.IsWorkspaceOwnerAsync(task.WorkspaceId, userId, ct);

                if (!isOwner)
                    return ApiResult.Fail("Chỉ owner được xóa", "FORBIDDEN");

                task.SoftDelete();
                await SaveChangesAsync(ct);

                await _activityLogService.LogActivityAsync(task.WorkspaceId, userId, "Delete", "WorkTask", task.Id);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    task.WorkspaceId,
                    "TaskDeleted",
                    new { TaskId = task.Id }
                );

                return ApiResult.Success(null, "Xóa task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi xóa task", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> CompleteTaskAsync(int taskId, CancellationToken ct = default)
        {
            return await ChangeStatus(taskId, StatusWorkTask.Done, ct);
        }

        public async Task<ApiResult> ReOpenTaskAsync(int taskId, CancellationToken ct = default)
        {
            return await ChangeStatus(taskId, StatusWorkTask.ToDo, ct);
        }

        public async Task<ApiResult> GetTaskByIdAsync(int taskId, CancellationToken ct = default)
        {
            var task = await _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            return task == null
                ? ApiResult.Fail("Không tìm thấy task")
                : ApiResult.Success(task);
        }

        public async Task<ApiResult> GetTasksByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            var query = _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId && !t.IsDeleted)
                .OrderByDescending(t => t.Id);

            return await GetPagedAsync(query, paging, ct);
        }

        public async Task<ApiResult> GetTasksByStatusAsync(int workspaceId, StatusWorkTask status, PagingRequest? paging = null, CancellationToken ct = default)
        {
            var query = _dataContext.Set<WorkTask>()
                .AsNoTracking()
                .Where(t => t.WorkspaceId == workspaceId && t.Status == status && !t.IsDeleted)
                .OrderByDescending(t => t.Id);

            return await GetPagedAsync(query, paging, ct);
        }

        // private

        private async Task<ApiResult> ChangeStatus(int taskId, StatusWorkTask status, CancellationToken ct)
        {
            var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

            var task = await _dataContext.Set<WorkTask>()
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

            if (task == null)
                return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

            if (!await _permissionService.HasWorkspaceAccessAsync(task.WorkspaceId, userId, ct))
                return ApiResult.Fail("Không có quyền", "FORBIDDEN");

            if (status == StatusWorkTask.Done)
            {
                task.Complete();
                await _taskPerformanceMetricService.UpdateMetricOnCompletionAsync(
                    new server.Service.Models.TaskPerformanceMetric.UpdateMetricOnCompletionModel
                    {
                        TaskId = taskId,
                        UserId = userId,
                        DurationMinutes = 0
                    });
            }
            else
            {
                task.ReOpen();
                await _taskPerformanceMetricService.UpdateMetricOnAbandonmentAsync(taskId, userId);
            }

            await SaveChangesAsync(ct);

            string action = status == StatusWorkTask.Done ? "Complete" : "Reopen";
            await _activityLogService.LogActivityAsync(task.WorkspaceId, userId, action, "WorkTask", task.Id);

            await ServiceHelper.SafeNotifyAsync(
                _notifier,
                task.WorkspaceId,
                "TaskStatusUpdated",
                new { TaskId = task.Id, Status = status }
            );

            return ApiResult.Success(task);
        }
    }
}