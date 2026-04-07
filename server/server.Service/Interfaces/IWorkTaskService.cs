using server.Domain.Enums;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IWorkTaskService: Quản lý công việc.
    /// </summary>
    public interface IWorkTaskService
    {
        /// <summary>
        /// Tạo task mới.
        /// </summary>
        Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, CancellationToken ct = default);

        /// <summary>
        /// Cập nhật thông tin task.
        /// </summary>
        Task<ApiResult> UpdateTaskAsync(UpdateWorkTaskModel model, CancellationToken ct = default);

        /// <summary>
        /// Xóa task.
        /// </summary>
        Task<ApiResult> DeleteTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Đánh dấu hoàn thành task.
        /// </summary>
        Task<ApiResult> CompleteTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Mở lại task đã xong.
        /// </summary>
        Task<ApiResult> ReOpenTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách task được gợi ý (theo Priority giảm dần, sau đó DueDate tăng dần).
        /// </summary>
        Task<ApiResult> GetRecommendedTasksAsync(int workspaceId, int limit = 5, CancellationToken ct = default);

        /// <summary>
        /// Lấy task cụ thể.
        /// </summary>
        Task<ApiResult> GetTaskByIdAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy tất cả task trong workspace (Hỗ trợ phân trang).
        /// </summary>
        Task<ApiResult> GetTasksByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy task theo trạng thái (To Do, In Progress, Done).
        /// </summary>
        Task<ApiResult> GetTasksByStatusAsync(int workspaceId, StatusWorkTask status, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy task quá hạn. Sắp xếp theo DueDate.
        /// </summary>
        Task<ApiResult> GetOverdueTasksAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Tìm kiếm task theo từ khóa.
        /// </summary>
        Task<ApiResult> SearchTasksAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy thống kê hiệu suất hoàn thành task trong workspace (Dùng cho Dashboard).
        /// </summary>
        Task<ApiResult> GetTaskStatisticsAsync(int workspaceId, CancellationToken ct = default);
    }
}