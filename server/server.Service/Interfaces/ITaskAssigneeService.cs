using server.Service.Models;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskAssigneeService: Quản lý gán task cho user.
    /// </summary>
    public interface ITaskAssigneeService
    {
        /// <summary>
        /// Gán task cho user.
        /// </summary>
        Task<ApiResult> AssignTaskAsync(int taskId, int userId);

        /// <summary>
        /// Bỏ gán task từ user.
        /// </summary>
        Task<ApiResult> UnassignTaskAsync(int taskId, int userId);

        /// <summary>
        /// Lấy tất cả user được gán task.
        /// </summary>
        Task<ApiResult> GetAssigneesAsync(int taskId);

        /// <summary>
        /// Lấy tất cả task assigned cho user.
        /// </summary>
        Task<ApiResult> GetAssignedTasksAsync(int userId);

        /// <summary>
        /// Lấy tất cả task assigned cho user trong workspace.
        /// </summary>
        Task<ApiResult> GetAssignedTasksByWorkspaceAsync(int userId, int workspaceId);

        /// <summary>
        /// Kiểm tra user được gán task không.
        /// </summary>
        Task<ApiResult> IsAssignedAsync(int taskId, int userId);

        /// <summary>
        /// Lấy task assigned cho multiple users.
        /// </summary>
        Task<ApiResult> GetAssignedTasksByUsersAsync(List<int> userIds);

        /// <summary>
        /// Cập nhật người gán task (replace current assignee).
        /// </summary>
        Task<ApiResult> ReassignTaskAsync(int taskId, int oldUserId, int newUserId);

        /// <summary>
        /// Gán task cho nhiều user cùng lúc.
        /// </summary>
        Task<ApiResult> AssignTaskToMultipleUsersAsync(int taskId, List<int> userIds);

        /// <summary>
        /// Bỏ gán task từ tất cả user.
        /// </summary>
        Task<ApiResult> UnassignTaskFromAllAsync(int taskId);
    }
}
