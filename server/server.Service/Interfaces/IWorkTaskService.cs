using server.Domain.Enums;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IWorkTaskService: Quản lý công việc thông minh với thuật toán gợi ý.
    /// </summary>
    public interface IWorkTaskService
    {

        /// Tạo task mới. Mặc định tính toán trọng số ban đầu dựa trên Priority.
        Task<ApiResult> CreateTaskAsync(AddWorkTaskModel model, CancellationToken ct = default);

        /// Cập nhật thông tin task. Tự động tính lại trọng số nếu thay đổi Priority.
        Task<ApiResult> UpdateTaskAsync(UpdateWorkTaskModel model, CancellationToken ct = default);

        /// Xóa task.
        Task<ApiResult> DeleteTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Đánh dấu hoàn thành task. 
        /// Thực hiện: Tăng CompletionCount, tính lại RecommendationWeight và OptimalHour.
        /// </summary>
        /// <param name="durationMinutes">Thời gian user thực hiện task (phút)</param>
        Task<ApiResult> CompleteTaskAsync(int taskId, int durationMinutes, CancellationToken ct = default);

        /// <summary>
        /// Mở lại task đã xong.
        /// </summary>
        Task<ApiResult> ReOpenTaskAsync(int taskId, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách task được GỢI Ý (Smart Recommendations).
        /// Thuật toán: Sắp xếp theo RecommendationWeight giảm dần và ưu tiên task có OptimalHour gần với giờ hiện tại.
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
        /// Lấy task quá hạn. Sắp xếp theo độ trễ (Overdue duration).
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