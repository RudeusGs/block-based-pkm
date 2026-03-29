using server.Service.Models;
using server.Service.Models.Page;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IPageService: Quản lý trang tài liệu trong workspace.
    /// </summary>
    public interface IPageService
    {
        /// <summary>
        /// Tạo trang mới trong workspace.
        /// </summary>
        Task<ApiResult> CreatePageAsync(AddPageModel model, int userId);

        /// <summary>
        /// Cập nhật thông tin trang.
        /// </summary>
        Task<ApiResult> UpdatePageAsync(UpdatePageModel model);

        /// <summary>
        /// Xóa trang (soft delete).
        /// </summary>
        Task<ApiResult> DeletePageAsync(int pageId);

        /// <summary>
        /// Lấy tất cả trang trong workspace.
        /// </summary>
        Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId);

        /// <summary>
        /// Lấy thông tin trang cụ thể.
        /// </summary>
        Task<ApiResult> GetPageByIdAsync(int pageId);

        /// <summary>
        /// Lấy tất cả task trong trang.
        /// </summary>
        Task<ApiResult> GetTasksByPageAsync(int pageId);

        /// <summary>
        /// Tìm kiếm trang theo từ khóa.
        /// </summary>
        Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword);

    }
}
