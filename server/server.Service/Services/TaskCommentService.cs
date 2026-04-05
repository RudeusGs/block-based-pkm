using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Realtime.Interfaces;
using server.Service.Common.Helpers;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskComment;

namespace server.Service.Services
{
    public class TaskCommentService : BaseService, ITaskCommentService
    {
        private readonly IRealtimeNotifier _notifier;

        public TaskCommentService(
            DataContext dataContext,
            IUserService userService,
            IRealtimeNotifier notifier
        ) : base(dataContext, userService)
        {
            _notifier = notifier;
        }

        // ================= ADD =================
        public async Task<ApiResult> AddCommentAsync(AddCommentModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || model.TaskId <= 0 || string.IsNullOrWhiteSpace(model.Content))
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

                var comment = new TaskComment(model.TaskId, userId, model.Content);

                _dataContext.Set<TaskComment>().Add(comment);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    workspace.Id,
                    "TaskCommentAdded",
                    new
                    {
                        TaskId = model.TaskId,
                        CommentId = comment.Id,
                        UserId = userId,
                        Content = comment.Content
                    });

                return ApiResult.Success(comment, "Thêm bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi thêm bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdateCommentAsync(UpdateCommentModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || model.CommentId <= 0 || string.IsNullOrWhiteSpace(model.Content))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var comment = await _dataContext.Set<TaskComment>()
                    .FirstOrDefaultAsync(c => c.Id == model.CommentId, ct);

                if (comment == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == comment.TaskId, ct);

                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);

                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (comment.UserId != userId && workspace.OwnerId != userId)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                comment.UpdateContent(model.Content, userId);

                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    workspace.Id,
                    "TaskCommentUpdated",
                    new
                    {
                        TaskId = comment.TaskId,
                        CommentId = comment.Id,
                        Content = comment.Content
                    });

                return ApiResult.Success(comment, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> DeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            try
            {
                if (commentId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var comment = await _dataContext.Set<TaskComment>()
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct);

                if (comment == null)
                    return ApiResult.Fail("NOT_FOUND");

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == comment.TaskId, ct);

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);

                if (comment.UserId != userId &&
                    task.CreatedById != userId &&
                    workspace.OwnerId != userId)
                    return ApiResult.Fail("FORBIDDEN");

                _dataContext.Set<TaskComment>().Remove(comment);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(
                    _notifier,
                    workspace.Id,
                    "TaskCommentDeleted",
                    new { TaskId = comment.TaskId, CommentId = commentId });

                return ApiResult.Success(null, "Xóa thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                    "Có lỗi xảy ra",
                    "SERVER_ERROR",
                    new[] { ex.ToString() }
                );
            }
        }

        public async Task<ApiResult> GetTaskCommentsAsync(int taskId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var task = await _dataContext.Set<WorkTask>()
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);

                var workspace = await _dataContext.Set<Workspace>()
                    .FirstOrDefaultAsync(w => w.Id == task.WorkspaceId, ct);

                var isMember = await _dataContext.Set<WorkspaceMember>()
                    .AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId, ct);

                if (!isMember && workspace.OwnerId != userId)
                    return ApiResult.Fail("FORBIDDEN");

                IQueryable<TaskComment> query = _dataContext.Set<TaskComment>()
                    .AsNoTracking()
                    .Where(c => c.TaskId == taskId)
                    .OrderBy(c => c.CreatedDate);

                if (paging != null)
                {
                    query = query.Skip((paging.PageNumber - 1) * paging.PageSize)
                                 .Take(paging.PageSize);
                }

                var items = await query.ToListAsync(ct);

                return ApiResult.Success(items, "OK");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(
                  "Có lỗi xảy ra",
                  "SERVER_ERROR",
                  new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetCommentByIdAsync(int commentId, CancellationToken ct = default)
        {
            var comment = await _dataContext.Set<TaskComment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commentId, ct);

            if (comment == null)
                return ApiResult.Fail("NOT_FOUND");

            return ApiResult.Success(comment, "OK");
        }

        public async Task<ApiResult> GetCommentCountAsync(int taskId, CancellationToken ct = default)
        {
            var count = await _dataContext.Set<TaskComment>()
                .AsNoTracking()
                .CountAsync(c => c.TaskId == taskId, ct);

            return ApiResult.Success(count, "OK");
        }

        public async Task<ApiResult> GetCommentsByUserAsync(int userId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            IQueryable<TaskComment> query = _dataContext.Set<TaskComment>()
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedDate);

            if (paging != null)
            {
                query = query.Skip((paging.PageNumber - 1) * paging.PageSize)
                             .Take(paging.PageSize);
            }

            var items = await query.ToListAsync(ct);

            return ApiResult.Success(items, "OK");
        }
    }
}