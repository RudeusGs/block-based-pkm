using server.Service.Models;
using server.Service.Models.UserTaskHistory;

namespace server.Service.Interfaces
{
    /// <summary>
    /// IUserTaskHistoryService: Quản lý lịch sử hoàn thành task của user.
    /// Dùng để tracking task completion history cho thuật toán gợi ý.
    /// </summary>
    public interface IUserTaskHistoryService
    {
        /// <summary>
        /// Record task hoàn thành.
        /// </summary>
        Task<ApiResult> RecordCompletionAsync(RecordTaskCompletionModel model, int userId);

        /// <summary>
        /// Record task bị abandon (user bắt đầu nhưng không hoàn thành).
        /// </summary>
        Task<ApiResult> RecordAbandonmentAsync(RecordAbandonmentModel model, int userId);

        /// <summary>
        /// Record task skipped (user skip task).
        /// </summary>
        Task<ApiResult> RecordSkipAsync(RecordSkipModel model, int userId);

        /// <summary>
        /// Lấy tất cả lịch sử task của user.
        /// </summary>
        Task<ApiResult> GetUserTaskHistoryAsync(int userId);

        /// <summary>
        /// Lấy lịch sử task của user trong workspace.
        /// </summary>
        Task<ApiResult> GetUserTaskHistoryByWorkspaceAsync(int userId, int workspaceId);

        /// <summary>
        /// Lấy lịch sử hoàn thành của task.
        /// </summary>
        Task<ApiResult> GetTaskHistoryAsync(int taskId);

        /// <summary>
        /// Lấy lịch sử task trong khoảng thời gian.
        /// </summary>
        Task<ApiResult> GetHistoryByDateRangeAsync(GetHistoryDateRangeModel model);

        /// <summary>
        /// Lấy thời gian hoàn thành trung bình của task.
        /// </summary>
        Task<ApiResult> GetAverageDurationAsync(int taskId);

        /// <summary>
        /// Lấy thời gian hoàn thành trung bình của user.
        /// </summary>
        Task<ApiResult> GetUserAverageDurationAsync(int userId);

        /// <summary>
        /// Lấy tần suất hoàn thành task (bao lần).
        /// </summary>
        Task<ApiResult> GetCompletionCountAsync(int taskId);

        /// <summary>
        /// Lấy tỷ lệ hoàn thành task.
        /// </summary>
        Task<ApiResult> GetCompletionRateAsync(int taskId);

        /// <summary>
        /// Xóa lịch sử (cleanup cũ).
        /// </summary>
        Task<ApiResult> DeleteHistoryAsync(int historyId);

        /// <summary>
        /// Lấy hoàn thành gần đây nhất.
        /// </summary>
        Task<ApiResult> GetRecentCompletionsAsync(int userId, int limit = 10);
    }
}
