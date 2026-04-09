using Microsoft.EntityFrameworkCore;
using server.Domain.Base;
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

        public async Task<ApiResult> AddCommentAsync(AddCommentModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || model.TaskId <= 0 || string.IsNullOrWhiteSpace(model.Content))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var ctx = await GetTaskContextAsync(model.TaskId, userId, ct);
                if (ctx.Error != null) return ctx.Error;

                TaskComment? parent = null;
                if (model.ParentId.HasValue)
                {
                    parent = await _dataContext.Set<TaskComment>()
                        .FirstOrDefaultAsync(c => c.Id == model.ParentId.Value, ct);

                    if (parent == null)
                        return ApiResult.Fail("Parent comment không tồn tại", "NOT_FOUND");
                }

                var comment = new TaskComment(model.TaskId, userId, model.Content, parent);
                _dataContext.Set<TaskComment>().Add(comment);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, ctx.WorkspaceId, "TaskCommentAdded", new
                {
                    TaskId    = model.TaskId,
                    CommentId = comment.Id,
                    ParentId  = comment.ParentId,
                    UserId    = userId,
                    Content   = comment.Content
                });

                return ApiResult.Success(comment, "Thêm bình luận thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi khi thêm bình luận", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> UpdateCommentAsync(int commentId, UpdateCommentModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Content))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var comment = await _dataContext.Set<TaskComment>()
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct);

                if (comment == null || comment.IsDeleted)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var ctx = await GetTaskContextAsync(comment.TaskId, userId, ct);
                if (ctx.Error != null) return ctx.Error;

                comment.UpdateContent(model.Content, userId);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, ctx.WorkspaceId, "TaskCommentUpdated", new
                {
                    TaskId    = comment.TaskId,
                    CommentId = comment.Id,
                    Content   = comment.Content
                });

                return ApiResult.Success(comment, "Cập nhật thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi khi cập nhật", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> SoftDeleteCommentAsync(int commentId, CancellationToken ct = default)
        {
            try
            {
                if (commentId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var comment = await _dataContext.Set<TaskComment>()
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct);

                if (comment == null || comment.IsDeleted)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var ctx = await GetTaskContextAsync(comment.TaskId, userId, ct);
                if (ctx.Error != null) return ctx.Error;

                if (comment.UserId != userId && !ctx.IsOwner)
                    return ApiResult.Fail("Không có quyền", "FORBIDDEN");

                comment.SoftDelete(comment.UserId);
                await SaveChangesAsync(ct);

                await ServiceHelper.SafeNotifyAsync(_notifier, ctx.WorkspaceId, "TaskCommentDeleted", new
                {
                    TaskId    = comment.TaskId,
                    CommentId = commentId
                });

                return ApiResult.Success(null, "Xóa thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi xảy ra", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> RestoreCommentAsync(int commentId, CancellationToken ct = default)
        {
            try
            {
                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var comment = await _dataContext.Set<TaskComment>()
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct);

                if (comment == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                if (!comment.IsDeleted)
                    return ApiResult.Fail("Comment chưa bị xóa", "INVALID_STATE");

                comment.Restore(userId);
                await SaveChangesAsync(ct);

                return ApiResult.Success(comment, "Khôi phục thành công");
            }
            catch (DomainException ex) { return ApiResult.Fail(ex.Message, "VALIDATION_ERROR"); }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi khi khôi phục", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetTaskCommentsAsync(int taskId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);
                var ctx = await GetTaskContextAsync(taskId, userId, ct);
                if (ctx.Error != null) return ctx.Error;

                IQueryable<TaskComment> q = _dataContext.Set<TaskComment>()
                    .AsNoTracking()
                    .Where(c => c.TaskId == taskId && c.ParentId == null)
                    .OrderBy(c => c.CreatedDate);

                if (paging != null)
                    q = q.Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize);

                return ApiResult.Success(await q.ToListAsync(ct), "OK");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi xảy ra", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetRepliesAsync(int parentCommentId, CancellationToken ct = default)
        {
            try
            {
                if (parentCommentId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var userId = ServiceHelper.GetCurrentUserIdOrThrow(_userService);

                var parent = await _dataContext.Set<TaskComment>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == parentCommentId, ct);

                if (parent == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var ctx = await GetTaskContextAsync(parent.TaskId, userId, ct);
                if (ctx.Error != null) return ctx.Error;

                var replies = await _dataContext.Set<TaskComment>()
                    .AsNoTracking()
                    .Where(c => c.ParentId == parentCommentId)
                    .OrderBy(c => c.CreatedDate)
                    .ToListAsync(ct);

                return ApiResult.Success(replies, "OK");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi xảy ra", "SERVER_ERROR"); }
        }

        public async Task<ApiResult> GetCommentByIdAsync(int commentId, CancellationToken ct = default)
        {
            var comment = await _dataContext.Set<TaskComment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commentId, ct);

            return comment == null
                ? ApiResult.Fail("Comment không tồn tại", "NOT_FOUND")
                : ApiResult.Success(comment);
        }

        public async Task<ApiResult> GetCommentCountAsync(int taskId, CancellationToken ct = default)
        {
            var count = await _dataContext.Set<TaskComment>()
                .AsNoTracking()
                .CountAsync(c => c.TaskId == taskId && !c.IsDeleted, ct);

            return ApiResult.Success(count);
        }

        public async Task<ApiResult> GetCommentsByUserAsync(int userId, PagingRequest? paging = null, CancellationToken ct = default)
        {
            try
            {
                if (userId <= 0)
                    return ApiResult.Fail("INVALID_INPUT");

                var pageNumber = paging?.PageNumber > 0 ? paging.PageNumber : 1;
                var pageSize = paging?.PageSize > 0 ? Math.Min(paging.PageSize, 100) : 10;

                var query = _dataContext.Set<TaskComment>()
                    .AsNoTracking()
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedDate);

                var total = await query.CountAsync(ct);
                var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

                return ApiResult.Success(new { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize }, "OK");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception) { return ApiResult.Fail("Có lỗi xảy ra", "SERVER_ERROR"); }
        }


        private record TaskContext(int WorkspaceId, bool IsOwner, ApiResult? Error);

        private async Task<TaskContext> GetTaskContextAsync(int taskId, int userId, CancellationToken ct)
        {
            var result = await (
                from t  in _dataContext.Set<WorkTask>()
                join w  in _dataContext.Set<Workspace>() on t.WorkspaceId equals w.Id
                where t.Id == taskId
                select new { WorkspaceId = w.Id, OwnerId = w.OwnerId }
            ).AsNoTracking().FirstOrDefaultAsync(ct);

            if (result == null)
                return new TaskContext(0, false, ApiResult.Fail("Task không tồn tại", "NOT_FOUND"));

            var isOwner   = result.OwnerId == userId;
            var isMember  = isOwner || await _dataContext.Set<WorkspaceMember>()
                .AnyAsync(m => m.WorkspaceId == result.WorkspaceId && m.UserId == userId, ct);

            if (!isMember)
                return new TaskContext(0, false, ApiResult.Fail("Không có quyền", "FORBIDDEN"));

            return new TaskContext(result.WorkspaceId, isOwner, null);
        }
    }
}