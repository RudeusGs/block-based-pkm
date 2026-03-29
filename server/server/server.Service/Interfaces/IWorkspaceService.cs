using server.Service.Models;
using server.Service.Models.Workspace;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IWorkspaceService: Quản lý không gian làm việc của user.
    /// (CRUD + lấy dữ liệu workspace theo user hoặc id)
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// Tạo mới một workspace.
        /// </summary>
        Task<ApiResult> CreateWorkspaceAsync(AddWorkspaceModel model);

        /// <summary>
        /// Cập nhật thông tin workspace.
        /// </summary>
        Task<ApiResult> UpdateWorkspaceAsync(UpdateWorkspaceModel model);

        /// <summary>
        /// Xóa workspace theo id (soft delete).
        /// </summary>
        Task<ApiResult> DeleteWorkspaceAsync(int workspaceId);

        /// <summary>
        /// Lấy tất cả workspace mà user tham gia hoặc sở hữu.
        /// </summary>
        Task<ApiResult> GetAllByUserIdAsync(int userId);

        /// <summary>
        /// Lấy thông tin chi tiết của một workspace theo id.
        /// </summary>
        Task<ApiResult> GetWorkspaceByIdAsync(int id);
    }
}