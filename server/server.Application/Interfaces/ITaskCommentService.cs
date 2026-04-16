using server.Service.Models;
using server.Service.Models.TaskComment;

namespace server.Service.Interfaces
{
    /// <summary>
    /// Quản lý bình luận trên task
    /// </summary>
    public interface ITaskCommentService
    {
        Task<ApiResult> AddCommentAsync(AddCommentModel model, CancellationToken ct = default);
        Task<ApiResult> UpdateCommentAsync(int commentId, UpdateCommentModel model, CancellationToken ct = default);

        Task<ApiResult> SoftDeleteCommentAsync(int commentId, CancellationToken ct = default);

        Task<ApiResult> RestoreCommentAsync(int commentId, CancellationToken ct = default);

        Task<ApiResult> GetTaskCommentsAsync(int taskId, PagingRequest? paging = null, CancellationToken ct = default);

        Task<ApiResult> GetRepliesAsync(int parentCommentId, CancellationToken ct = default);

        Task<ApiResult> GetCommentByIdAsync(int commentId, CancellationToken ct = default);
        Task<ApiResult> GetCommentCountAsync(int taskId, CancellationToken ct = default);
        Task<ApiResult> GetCommentsByUserAsync(int userId, PagingRequest? paging = null, CancellationToken ct = default);
    }
}