using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskComment;
using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Realtime.Interfaces;

namespace server.Service.Services
{
    public class TaskCommentService : BaseService, ITaskCommentService
    {
        private readonly IRealtimeNotifier _notifier;

        public TaskCommentService(DataContext dataContext, IUserService userService,
            IRealtimeNotifier notifier) : base(dataContext, userService)
        {
            _notifier = notifier;
        }

        public async Task<ApiResult> AddCommentAsync(AddCommentModel model, int userId)
        {
            try
            {
                if (model == null || model.TaskId <= 0 || string.IsNullOrWhiteSpace(model.Content))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0 || currentUserId != userId)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == model.TaskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId);
                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền bình luận trong workspace này", "FORBIDDEN");

                var comment = new TaskComment
                {
                    TaskId = model.TaskId,
                    UserId = currentUserId,
                    Content = model.Content.Trim(),
                    CreatedDate = Now
                };

                _dataContext.Set<TaskComment>().Add(comment);
                await SaveChangesAsync();

                try { await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskCommentAdded", new { TaskId = model.TaskId, CommentId = comment.Id, UserId = currentUserId, Content = comment.Content }); } catch { }

                return ApiResult.Success(comment, "Thêm bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi thêm bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> DeleteCommentAsync(int commentId)
        {
            try
            {
                if (commentId <= 0)
                    return ApiResult.Fail("CommentId không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var comment = await _dataContext.Set<TaskComment>().FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == comment.TaskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                if (comment.UserId != currentUserId && task.CreatedById != currentUserId && workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền xóa bình luận này", "FORBIDDEN");

                _dataContext.Set<TaskComment>().Remove(comment);
                await SaveChangesAsync();

                try { await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskCommentDeleted", new { TaskId = comment.TaskId, CommentId = commentId }); } catch { }

                return ApiResult.Success(null, "Xóa bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi xóa bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetCommentByIdAsync(int commentId)
        {
            try
            {
                if (commentId <= 0)
                    return ApiResult.Fail("CommentId không hợp lệ", "INVALID_INPUT");

                var comment = await _dataContext.Set<TaskComment>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == comment.TaskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId);
                var isCommenter = comment.UserId == currentUserId;
                if (!isOwner && !isMember && !isCommenter)
                    return ApiResult.Fail("Bạn không có quyền xem bình luận này", "FORBIDDEN");

                return ApiResult.Success(comment, "Lấy bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetCommentCountAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId);
                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem bình luận", "FORBIDDEN");

                var count = await _dataContext.Set<TaskComment>().AsNoTracking().CountAsync(c => c.TaskId == taskId);
                return ApiResult.Success(count, "Lấy số lượng bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy số lượng bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetCommentsByUserAsync(int userId)
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
                        return ApiResult.Fail("Bạn không có quyền xem bình luận của user này", "FORBIDDEN");
                }

                var comments = await _dataContext.Set<TaskComment>().AsNoTracking().Where(c => c.UserId == userId).ToListAsync();
                return ApiResult.Success(comments, "Lấy bình luận của user thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> GetTaskCommentsAsync(int taskId)
        {
            try
            {
                if (taskId <= 0)
                    return ApiResult.Fail("TaskId không hợp lệ", "INVALID_INPUT");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");

                var isOwner = workspace.OwnerId == currentUserId;
                var isMember = await _dataContext.Set<WorkspaceMember>().AsNoTracking().AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == currentUserId);
                if (!isOwner && !isMember)
                    return ApiResult.Fail("Bạn không có quyền xem bình luận trong workspace này", "FORBIDDEN");

                var comments = await _dataContext.Set<TaskComment>().AsNoTracking().Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedDate).ToListAsync();
                return ApiResult.Success(comments, "Lấy danh sách bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi lấy bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }

        public async Task<ApiResult> UpdateCommentAsync(UpdateCommentModel model)
        {
            try
            {
                if (model == null || model.CommentId <= 0 || string.IsNullOrWhiteSpace(model.Content))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var currentUserId = _userService.UserId;
                if (currentUserId <= 0)
                    return ApiResult.Fail("Unauthorized", "UNAUTHORIZED");

                var comment = await _dataContext.Set<TaskComment>().FirstOrDefaultAsync(c => c.Id == model.CommentId);
                if (comment == null)
                    return ApiResult.Fail("Comment không tồn tại", "NOT_FOUND");

                var task = await _dataContext.Set<WorkTask>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == comment.TaskId);
                if (task == null)
                    return ApiResult.Fail("Task không tồn tại", "NOT_FOUND");

                var workspace = await _dataContext.Set<Workspace>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == task.WorkspaceId);
                if (workspace == null)
                    return ApiResult.Fail("Workspace không tồn tại", "NOT_FOUND");
                if (comment.UserId != currentUserId && workspace.OwnerId != currentUserId)
                    return ApiResult.Fail("Bạn không có quyền cập nhật bình luận này", "FORBIDDEN");

                comment.Content = model.Content.Trim();
                comment.MarkUpdated();
                _dataContext.Set<TaskComment>().Update(comment);
                await SaveChangesAsync();

                try { await _notifier.SendToWorkspaceAsync(workspace.Id, "TaskCommentUpdated", new { TaskId = comment.TaskId, CommentId = comment.Id, Content = comment.Content }); } catch { }

                return ApiResult.Success(comment, "Cập nhật bình luận thành công");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Có lỗi khi cập nhật bình luận", "SERVER_ERROR", new[] { ex.ToString() });
            }
        }
    }
}
