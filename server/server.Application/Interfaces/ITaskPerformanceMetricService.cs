using server.Service.Models;
using server.Service.Models.TaskPerformanceMetric;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskPerformanceMetricService: Quản lý chỉ số hiệu suất của task.
    /// </summary>
    public interface ITaskPerformanceMetricService
    {
        Task<ApiResult> CreateMetricAsync(int taskId, int userId, int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetMetricAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> UpdateMetricOnCompletionAsync(UpdateMetricOnCompletionModel model, CancellationToken ct = default);

        Task<ApiResult> UpdateMetricOnAbandonmentAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetCompletionRateAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetAverageDurationAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetOptimalCompletionHourAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetOptimalDayOfWeekAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetCompletionTrendAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> GetDaysSinceLastCompletionAsync(int taskId, int userId, CancellationToken ct = default);

        Task<ApiResult> RecalculateAllMetricsAsync(int workspaceId, CancellationToken ct = default);

        Task<ApiResult> GetTopPerformingTasksAsync(int userId, int workspaceId, int limit = 10, CancellationToken ct = default);

        Task<ApiResult> GetLowPerformingTasksAsync(int userId, int workspaceId, int limit = 10, CancellationToken ct = default);
    }
}
