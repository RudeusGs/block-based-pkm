using server.Infrastructure.Realtime.Interfaces;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Services
{
    public class WorkTaskService : BaseService, IWorkTaskService
    {
        private readonly IRealtimeNotifier _notifier;

        public WorkTaskService(DataContext dataContext, IUserService userService,
            IRealtimeNotifier notifier) : base(dataContext, userService)
        {
            _notifier = notifier;
        }
        public async Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, int userId)
        {
            try
            {
                if (model == null)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                if (model.WorkspaceId <= 0 || string.IsNullOrWhiteSpace(model.Title))
                    return ApiResult.Fail("Workspace hoặc title không hợp lệ", "VALIDATION_ERROR");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0 || currentUserId != userId)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == model.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == model.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền tạo task trong workspace này", "FORBIDDEN");

                var task = new WorkTask
                {
                    WorkspaceId = model.WorkspaceId,
                    PageId = model.PageId,
                    Title = model.Title.Trim(),
                    Description = model.Description?.Trim(),
                    Priority = model.Priority,
                    DueDate = model.DueDate,
                    CreatedById = currentUserId,
                    CreatedDate = Now
                };

                _dataContext.Set<WorkTask>().Add(task);
                await SaveChangesAsync();

                try 
                { 
                    await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskCreated", 
                        new { TaskId = task.Id, WorkspaceId = task.WorkspaceId, Title = task.Title, CreatedBy = task.CreatedById }); 
                } 
                catch(Exception ex)
                { 
                    return ApiResult.Success(task, "Tạo task oke nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(task, "Tạo task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tạo task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> DeleteTaskAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId && task.CreatedById != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền xóa task này", "FORBIDDEN");

                _dataContext.Set<WorkTask>().Remove(task);
                await SaveChangesAsync();

                try 
                { 
                    await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskDeleted", 
                        new { TaskId = taskId, WorkspaceId = workspace.Id }); 
                } 
                catch(Exception ex)
                { 
                    return ApiResult.Success(null, "Xóa task oke nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(null, "Xóa task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi xóa task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetAssignedTasksAsync(int workspaceId, int userId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var assignedIdsQuery = _dataContext.Set<TaskAssignee>().AsNoTracking().Where(a => a.UserId == userId).Select(a => a.TaskId);

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId && assignedIdsQuery.Contains(t.Id));

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task được phân công thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetOverdueTasksAsync(int workspaceId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var now = Now;
                var q = _dataContext.Set<WorkTask>().AsNoTracking()
                    .Where(t => t.WorkspaceId == workspaceId && t.DueDate.HasValue && t.DueDate < now && !string.Equals(t.Status, "Done", StringComparison.OrdinalIgnoreCase));

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderBy(t => t.DueDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task quá hạn thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTaskByIdAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var task = await _dataContext.Set<WorkTask>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == task.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task này", "FORBIDDEN");

                return ApiResult.Success(task, "Lấy task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByCreatorAsync(int workspaceId, int userId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0 || userId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId && t.CreatedById == userId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task của người tạo thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByPageAsync(int pageId, PagingRequest? paging = null)
        {
            try
            {
                if (pageId <= 0)
                    return ApiResult.Fail("PageId không hợp lệ", "INVALID_INPUT");

                var page = await _dataContext.Set<Page>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pageId);

                if (page == null)
                    return ApiResult.Fail("Page không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == page.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == page.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong page này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.PageId == pageId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByStatusAsync(int workspaceId, string status, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0 || string.IsNullOrWhiteSpace(status))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId && t.Status == status);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task theo trạng thái thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksByWorkspaceAsync(int workspaceId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksSortedByDueDateAsync(int workspaceId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderBy(t => t.DueDate == null)
                    .ThenBy(t => t.DueDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task theo due date thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTasksSortedByPriorityAsync(int workspaceId, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId);

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderBy(t => t.Priority == "High" ? 0 : t.Priority == "Medium" ? 1 : t.Priority == "Low" ? 2 : 3)
                    .ThenByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task theo priority thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetUpcomingTasksAsync(int workspaceId, int days = 7, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem task trong workspace này", "FORBIDDEN");

                var to = Now.AddDays(days);
                var q = _dataContext.Set<WorkTask>().AsNoTracking()
                    .Where(t => t.WorkspaceId == workspaceId && t.DueDate.HasValue && t.DueDate >= Now && t.DueDate <= to && !string.Equals(t.Status, "Done", StringComparison.OrdinalIgnoreCase));

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderBy(t => t.DueDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Lấy danh sách task sắp đến hạn thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy danh sách task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> SearchTasksAsync(int workspaceId, string keyword, PagingRequest? paging = null)
        {
            try
            {
                if (workspaceId <= 0)
                    return ApiResult.Fail("WorkspaceId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == workspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền tìm kiếm task trong workspace này", "FORBIDDEN");

                var q = _dataContext.Set<WorkTask>().AsNoTracking().Where(t => t.WorkspaceId == workspaceId);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim().ToLowerInvariant();
                    q = q.Where(t => t.Title.ToLower().Contains(kw) || (t.Description != null && t.Description.ToLower().Contains(kw)));
                }

                paging ??= new PagingRequest();
                var total = await q.CountAsync();
                var items = await q.OrderByDescending(t => t.CreatedDate)
                    .Skip((paging.PageNumber - 1) * paging.PageSize)
                    .Take(paging.PageSize)
                    .ToListAsync();

                return ApiResult.Success(new { Total = total, Items = items }, "Tìm kiếm task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi tìm kiếm task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdateTaskAsync(UpdateWorkTaskModel model)
        {
            try
            {
                if (model == null || model.TaskId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == model.TaskId);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (workspace.OwnerId != currentUserId && task.CreatedById != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền cập nhật task này", "FORBIDDEN");

                if (!string.IsNullOrWhiteSpace(model.Title))
                    task.Title = model.Title.Trim();

                if (model.Description != null)
                    task.Description = model.Description.Trim();

                if (!string.IsNullOrWhiteSpace(model.Status))
                    task.Status = model.Status.Trim();

                if (!string.IsNullOrWhiteSpace(model.Priority))
                    task.Priority = model.Priority.Trim();

                task.DueDate = model.DueDate;

                task.MarkUpdated();
                _dataContext.Set<WorkTask>().Update(task);
                await SaveChangesAsync();
                try
                {
                    await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskUpdated", 
                        new { TaskId = task.Id, WorkspaceId = workspace.Id, Title = task.Title });
                }
                catch (Exception ex)
                {
                    return ApiResult.Success(null, "Cập nhật task oke la nhưng mà có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(task, "Cập nhật task thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdateTaskStatusAsync(int taskId, string newStatus)
        {
            try
            {
                if (taskId <= 0 || string.IsNullOrWhiteSpace(newStatus))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AsNoTracking()
                    .AnyAsync(m => m.WorkspaceId == task.WorkspaceId && m.UserId == currentUserId);

                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền cập nhật trạng thái task này", "FORBIDDEN");

                var normalizedNew = newStatus.Trim();
                var prevStatus = task.Status;
                task.Status = normalizedNew;

                if (!string.Equals(prevStatus, "Done", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(normalizedNew, "Done", StringComparison.OrdinalIgnoreCase))
                {
                    task.CompletionCount += 1;
                    task.LastCompletedAt = Now;
                }

                task.MarkUpdated();
                _dataContext.Set<WorkTask>().Update(task);
                await SaveChangesAsync();
                try
                {
                    await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskStatusUpdated", 
                        new { TaskId = task.Id, WorkspaceId = workspace.Id, Status = task.Status });
                }
                catch(Exception ex) 
                {
                    return ApiResult.Success(null, "Cập nhật trạng thái task oke, nhưng có lỗi khi gửi realtime notification: " + ex.Message);
                }

                return ApiResult.Success(task, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật trạng thái task", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }
    }
}
