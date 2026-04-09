using server.Domain.Enums;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Interfaces
{
    public interface IWorkTaskService
    {
        /// <summary>
        /// Tạo task mới
        /// </summary>
        Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, CancellationToken ct = default);

        /// <summary>
        /// Cập nhật thông tin task
        /// </summary>
        Task<ApiResult> UpdateTaskAsync(int TaskId, UpdateWorkTaskModel model, CancellationToken ct = default);

        /// <summary>
        /// Xóa mềm task
        /// </summary>
        Task<ApiResult> DeleteTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Đánh dấu hoàn thành
        /// </summary>
        Task<ApiResult> CompleteTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Mở lại task
        /// </summary>
        Task<ApiResult> ReOpenTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy task theo Id
        /// </summary>
        Task<ApiResult> GetTaskByIdAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách task theo workspace
        /// </summary>
        Task<ApiResult> GetTasksByWorkspaceAsync(
            int workspaceId,
            PagingRequest? paging = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lấy task theo trạng thái
        /// </summary>
        Task<ApiResult> GetTasksByStatusAsync(
            int workspaceId,
            StatusWorkTask status,
            PagingRequest? paging = null,
            CancellationToken ct = default);
    }
}