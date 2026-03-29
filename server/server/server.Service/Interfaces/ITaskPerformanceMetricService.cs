using server.Service.Models;
using server.Service.Models.TaskPerformanceMetric;

namespace server.Service.Interfaces
{
    /// <summary>
    /// ITaskPerformanceMetricService: Quản lý chỉ số hiệu suất của task.
    /// </summary>
    public interface ITaskPerformanceMetricService
    {
        /// <summary>
        /// Tạo metric mới cho task.
        /// </summary>
        Task<ApiResult> CreateMetricAsync(int taskId, int userId, int workspaceId);

        /// <summary>
        /// Lấy metric của task cho user.
        /// </summary>
        Task<ApiResult> GetMetricAsync(int taskId, int userId);

        /// <summary>
        /// Cập nhật metric sau khi hoàn thành task.
        /// </summary>
        Task<ApiResult> UpdateMetricOnCompletionAsync(UpdateMetricOnCompletionModel model);

        /// <summary>
        /// Cập nhật metric sau khi abandon task.
        /// </summary>
        Task<ApiResult> UpdateMetricOnAbandonmentAsync(int taskId, int userId);

        /// <summary>
        /// Lấy tỷ lệ hoàn thành task.
        /// </summary>
        Task<ApiResult> GetCompletionRateAsync(int taskId, int userId);

        /// <summary>
        /// Lấy thời gian hoàn thành trung bình.
        /// </summary>
        Task<ApiResult> GetAverageDurationAsync(int taskId, int userId);

        /// <summary>
        /// Lấy giờ tối ưu để hoàn thành task.
        /// </summary>
        Task<ApiResult> GetOptimalCompletionHourAsync(int taskId, int userId);

        /// <summary>
        /// Lấy ngày trong tuần tối ưu.
        /// </summary>
        Task<ApiResult> GetOptimalDayOfWeekAsync(int taskId, int userId);

        /// <summary>
        /// Lấy trend hoàn thành gần đây.
        /// </summary>
        Task<ApiResult> GetCompletionTrendAsync(int taskId, int userId);

        /// <summary>
        /// Lấy số ngày kể từ lần hoàn thành cuối.
        /// </summary>
        Task<ApiResult> GetDaysSinceLastCompletionAsync(int taskId, int userId);

        /// <summary>
        /// Tính lại tất cả metrics (cron job).
        /// </summary>
        Task<ApiResult> RecalculateAllMetricsAsync(int workspaceId);

        /// <summary>
        /// Lấy task có completion rate cao nhất.
        /// </summary>
        Task<ApiResult> GetTopPerformingTasksAsync(int userId, int workspaceId, int limit = 10);

        /// <summary>
        /// Lấy task có completion rate thấp nhất.
        /// </summary>
        Task<ApiResult> GetLowPerformingTasksAsync(int userId, int workspaceId, int limit = 10);
    }
}
