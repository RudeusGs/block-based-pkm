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
        Task<ApiResult> RecordCompletionAsync(RecordTaskCompletionModel model, int userId, CancellationToken ct = default);

        Task<ApiResult> RecordAbandonmentAsync(RecordAbandonmentModel model, int userId, CancellationToken ct = default);

        Task<ApiResult> RecordSkipAsync(RecordSkipModel model, int userId, CancellationToken ct = default);

        Task<ApiResult> GetUserTaskHistoryAsync(int userId, CancellationToken ct = default);

        Task<ApiResult> GetUserTaskHistoryByWorkspaceAsync(int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetTaskHistoryAsync(int taskId, CancellationToken ct = default);

        Task<ApiResult> GetHistoryByDateRangeAsync(GetHistoryDateRangeModel model, CancellationToken ct = default);

        Task<ApiResult> GetAverageDurationAsync(int taskId, CancellationToken ct = default);

        Task<ApiResult> GetUserAverageDurationAsync(int userId, CancellationToken ct = default);

        Task<ApiResult> GetCompletionCountAsync(int taskId, CancellationToken ct = default);

        Task<ApiResult> GetCompletionRateAsync(int taskId, CancellationToken ct = default);

        Task<ApiResult> DeleteHistoryAsync(int historyId, CancellationToken ct = default);

        Task<ApiResult> GetRecentCompletionsAsync(int userId, int limit = 10, CancellationToken ct = default);
    }
}
