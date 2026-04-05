using server.Service.Models;
using server.Service.Models.TaskComment;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskCommentService: Quản lý bình luận trên task.
    /// </summary>
    public interface ITaskCommentService
    {
        /// <summary>
        /// Thêm bình luận vào task.
        /// </summary>
        Task<ApiResult> AddCommentAsync(AddCommentModel model, CancellationToken ct = default);

        /// <summary>
        /// Cập nhật bình luận.
        /// </summary>
        Task<ApiResult> UpdateCommentAsync(UpdateCommentModel model, CancellationToken ct = default);

        /// <summary>
        /// Xóa bình luận.
        /// </summary>
        Task<ApiResult> DeleteCommentAsync(int commentId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả bình luận của task (hỗ trợ phân trang).
        /// </summary>
        Task<ApiResult> GetTaskCommentsAsync(int taskId, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy bình luận cụ thể.
        /// </summary>
        Task<ApiResult> GetCommentByIdAsync(int commentId, CancellationToken ct = default);

        /// <summary>
        /// Lấy bình luận được tạo bởi user (hỗ trợ phân trang).
        /// </summary>
        Task<ApiResult> GetCommentsByUserAsync(int userId, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy số lượng bình luận của task.
        /// </summary>
        Task<ApiResult> GetCommentCountAsync(int taskId, CancellationToken ct = default);
    }
}