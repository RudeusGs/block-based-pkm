using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;

namespace server.Controllers
{
    [Route("api/task-performance-metrics")]
    [Authorize]
    [ApiController]
    public class TaskPerformanceMetricController : BaseController
    {
        private readonly ITaskPerformanceMetricService _metricService;

        public TaskPerformanceMetricController(ITaskPerformanceMetricService metricService)
        {
            _metricService = metricService;
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}")]
        public async Task<IActionResult> GetMetric(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetMetricAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/completion-rate")]
        public async Task<IActionResult> GetCompletionRate(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetCompletionRateAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/average-duration")]
        public async Task<IActionResult> GetAverageDuration(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetAverageDurationAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/optimal-hour")]
        public async Task<IActionResult> GetOptimalCompletionHour(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetOptimalCompletionHourAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/optimal-day")]
        public async Task<IActionResult> GetOptimalDayOfWeek(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetOptimalDayOfWeekAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/trend")]
        public async Task<IActionResult> GetCompletionTrend(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetCompletionTrendAsync(taskId, userId));
        }

        [HttpGet("task/{taskId:int}/user/{userId:int}/days-since-last-completion")]
        public async Task<IActionResult> GetDaysSinceLastCompletion(int taskId, int userId)
        {
            return FromApiResult(await _metricService.GetDaysSinceLastCompletionAsync(taskId, userId));
        }

        [HttpPost("workspace/{workspaceId:int}/recalculate-all")]
        public async Task<IActionResult> RecalculateAllMetrics(int workspaceId)
        {
            return FromApiResult(await _metricService.RecalculateAllMetricsAsync(workspaceId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/top-performing")]
        public async Task<IActionResult> GetTopPerformingTasks(int userId, int workspaceId, [FromQuery] int limit = 10)
        {
            return FromApiResult(await _metricService.GetTopPerformingTasksAsync(userId, workspaceId, limit));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/low-performing")]
        public async Task<IActionResult> GetLowPerformingTasks(int userId, int workspaceId, [FromQuery] int limit = 10)
        {
            return FromApiResult(await _metricService.GetLowPerformingTasksAsync(userId, workspaceId, limit));
        }
    }
}
