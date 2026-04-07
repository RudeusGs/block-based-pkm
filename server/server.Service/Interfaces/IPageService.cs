using server.Service.Models;
using server.Service.Models.Page;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IPageService: Quản lý trang tài liệu (Notion-like) trong workspace.
    /// </summary>
    public interface IPageService
    {
        /// <summary>
        /// Tạo trang mới trong workspace.
        /// </summary>
        Task<ApiResult> CreatePageAsync(AddPageModel model, int userId, CancellationToken ct = default);

        /// <summary>
        /// Cập nhật thông tin trang (title).
        /// </summary>
        Task<ApiResult> UpdatePageAsync(UpdatePageModel model, CancellationToken ct = default);

        /// <summary>
        /// Xóa trang (soft delete - Archive).
        /// </summary>
        Task<ApiResult> DeletePageAsync(int pageId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả trang trong workspace (hỗ trợ phân trang, chỉ lấy trang chưa archive).
        /// </summary>
        Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy thông tin trang cụ thể (chỉ lấy trang chưa archive).
        /// </summary>
        Task<ApiResult> GetPageByIdAsync(int pageId, CancellationToken ct = default);

        /// <summary>
        /// Tìm kiếm trang theo từ khóa (hỗ trợ phân trang, chỉ lấy trang chưa archive).
        /// </summary>
        Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default);
    }
}