using server.Service.Models;
using server.Service.Models.WorkspaceMember;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IWorkspaceMemberService: Quản lý thành viên của workspace.
    /// </summary>
    public interface IWorkspaceMemberService
    {
        /// <summary>
        /// Thêm user vào workspace với role cụ thể.
        /// </summary>
        Task<ApiResult> AddMemberAsync(AddWorkspaceMemberModel model);

        /// <summary>
        /// Cập nhật role của thành viên trong workspace.
        /// </summary>
        Task<ApiResult> UpdateMemberRoleAsync(int workspaceId, int userId, string newRole);

        /// <summary>
        /// Xóa user khỏi workspace.
        /// </summary>
        Task<ApiResult> RemoveMemberAsync(int workspaceId, int userId);

        /// <summary>
        /// Lấy tất cả thành viên của workspace.
        /// </summary>
        Task<ApiResult> GetWorkspaceMembersAsync(int workspaceId);

    }
}
