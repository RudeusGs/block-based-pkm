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
        Task<ApiResult> AddCommentAsync(AddCommentModel model, int userId);

        /// <summary>
        /// Cập nhật bình luận.
        /// </summary>
        Task<ApiResult> UpdateCommentAsync(UpdateCommentModel model);

        /// <summary>
        /// Xóa bình luận.
        /// </summary>
        Task<ApiResult> DeleteCommentAsync(int commentId);

        /// <summary>
        /// Lấy tất cả bình luận của task.
        /// </summary>
        Task<ApiResult> GetTaskCommentsAsync(int taskId);

        /// <summary>
        /// Lấy bình luận cụ thể.
        /// </summary>
        Task<ApiResult> GetCommentByIdAsync(int commentId);

        /// <summary>
        /// Lấy bình luận được tạo bởi user.
        /// </summary>
        Task<ApiResult> GetCommentsByUserAsync(int userId);

        /// <summary>
        /// Lấy bình luận trong khoảng thời gian.
        /// </summary>
        Task<ApiResult> GetCommentsByDateRangeAsync(GetCommentDateRangeModel model);

        /// <summary>
        /// Tìm kiếm bình luận theo từ khóa.
        /// </summary>
        Task<ApiResult> SearchCommentsAsync(int taskId, string keyword);

        /// <summary>
        /// Lấy số lượng bình luận của task.
        /// </summary>
        Task<ApiResult> GetCommentCountAsync(int taskId);
    }
}
