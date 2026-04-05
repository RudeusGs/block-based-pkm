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
        Task<ApiResult> AssignTaskAsync(int taskId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Bỏ gán task từ user.
        /// </summary>
        Task<ApiResult> UnassignTaskAsync(int taskId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả user được gán task.
        /// </summary>
        Task<ApiResult> GetAssigneesAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả task assigned cho user.
        /// </summary>
        Task<ApiResult> GetAssignedTasksAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả task assigned cho user trong workspace.
        /// </summary>
        Task<ApiResult> GetAssignedTasksByWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra user được gán task không.
        /// </summary>
        Task<ApiResult> IsAssignedAsync(int taskId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Lấy task assigned cho multiple users.
        /// </summary>
        Task<ApiResult> GetAssignedTasksByUsersAsync(List<int> userIds, CancellationToken ct = default);

        /// <summary>
        /// Cập nhật người gán task (replace current assignee).
        /// </summary>
        Task<ApiResult> ReassignTaskAsync(int taskId, int oldUserId, int newUserId, CancellationToken ct = default);

        /// <summary>
        /// Gán task cho nhiều user cùng lúc.
        /// </summary>
        Task<ApiResult> AssignTaskToMultipleUsersAsync(int taskId, List<int> userIds, CancellationToken ct = default);

        /// <summary>
        /// Bỏ gán task từ tất cả user.
        /// </summary>
        Task<ApiResult> UnassignTaskFromAllAsync(int taskId, CancellationToken ct = default);
    }
}