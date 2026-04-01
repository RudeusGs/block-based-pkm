using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IWorkTaskService: Quản lý công việc trong workspace.
    /// </summary>
    public interface IWorkTaskService
    {
        /// <summary>
        /// Tạo task mới.
        /// </summary>
        Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, int userId);

        /// <summary>
        /// Cập nhật thông tin task.
        /// </summary>
        Task<ApiResult> UpdateTaskAsync(UpdateWorkTaskModel model);

        /// <summary>
        /// Xóa task.
        /// </summary>
        Task<ApiResult> DeleteTaskAsync(int taskId);

        /// <summary>
        /// Lấy tất cả task trong workspace.
        /// </summary>
        Task<ApiResult> GetTasksByWorkspaceAsync(int workspaceId, PagingRequest? paging = null);

        /// <summary>
        /// Lấy tất cả task trong page.
        /// </summary>
        Task<ApiResult> GetTasksByPageAsync(int pageId, PagingRequest? paging = null);

        /// <summary>
        /// Lấy task cụ thể.
        /// </summary>
        Task<ApiResult> GetTaskByIdAsync(int taskId);

        /// <summary>
        /// Cập nhật trạng thái task (To Do, Doing, Done).
        /// </summary>
        Task<ApiResult> UpdateTaskStatusAsync(int taskId, string newStatus);

        /// <summary>
        /// Lấy task theo trạng thái.
        /// </summary>
        Task<ApiResult> GetTasksByStatusAsync(int workspaceId, string status, PagingRequest? paging = null);

        /// <summary>
        /// Lấy task quá hạn (overdue).
        /// </summary>
        Task<ApiResult> GetOverdueTasksAsync(int workspaceId, PagingRequest? paging = null);

        /// <summary>
        /// Lấy task sắp đến hạn.
        /// </summary>
        Task<ApiResult> GetUpcomingTasksAsync(int workspaceId, int days = 7, PagingRequest? paging = null);

        /// <summary>
        /// Tìm kiếm task theo từ khóa.
        /// </summary>
        Task<ApiResult> SearchTasksAsync(int workspaceId, string keyword, PagingRequest? paging = null);

        /// <summary>
        /// Lấy task được tạo bởi user.
        /// </summary>
        Task<ApiResult> GetTasksByCreatorAsync(int workspaceId, int userId, PagingRequest? paging = null);

        /// <summary>
        /// Lấy task assigned cho user.
        /// </summary>
        Task<ApiResult> GetAssignedTasksAsync(int workspaceId, int userId, PagingRequest? paging = null);

        /// <summary>
        /// Sắp xếp task theo priority.
        /// </summary>
        Task<ApiResult> GetTasksSortedByPriorityAsync(int workspaceId, PagingRequest? paging = null);

        /// <summary>
        /// Sắp xếp task theo due date.
        /// </summary>
        Task<ApiResult> GetTasksSortedByDueDateAsync(int workspaceId, PagingRequest? paging = null);
    }
}
