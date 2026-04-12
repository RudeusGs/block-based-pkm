using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.ActivityLog;

namespace server.Controllers
{
    [Route("api/activity-logs")]
    [Authorize]
    [ApiController]
    public class ActivityLogController : BaseController
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetWorkspaceActivityLogs(int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetWorkspaceActivityLogsAsync(workspaceId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetUserActivityLogs(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetUserActivityLogsAsync(userId, workspaceId, ct));
        }

        [HttpGet("entity/{entityType}/{entityId:int}")]
        public async Task<IActionResult> GetEntityActivityLogs(string entityType, int entityId, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetEntityActivityLogsAsync(entityType, entityId, ct));
        }

        [HttpGet("workspace/{workspaceId:int}/action/{actionStr}")]
        public async Task<IActionResult> GetActivityLogsByAction(int workspaceId, string actionStr, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetActivityLogsByActionAsync(workspaceId, actionStr, ct));
        }

        [HttpPost("date-range")]
        public async Task<IActionResult> GetActivityLogsByDateRange([FromBody] GetActivityLogDateRangeModel model, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetActivityLogsByDateRangeAsync(model, ct));
        }

        [HttpGet("workspace/{workspaceId:int}/recent")]
        public async Task<IActionResult> GetRecentActivityLogs(int workspaceId, [FromQuery] int limit = 10, CancellationToken ct = default)
        {
            return FromApiResult(await _activityLogService.GetRecentActivityLogsAsync(workspaceId, limit, ct));
        }

        [HttpDelete("{logId:int}")]
        public async Task<IActionResult> DeleteActivityLog(int logId, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.DeleteActivityLogAsync(logId, ct));
        }

        [HttpDelete("workspace/{workspaceId:int}/old/{daysOld:int}")]
        public async Task<IActionResult> DeleteOldActivityLogs(int workspaceId, int daysOld, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.DeleteOldActivityLogsAsync(workspaceId, daysOld, ct));
        }

        [HttpGet("workspace/{workspaceId:int}/stats")]
        public async Task<IActionResult> GetActivityStats(int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _activityLogService.GetActivityStatsAsync(workspaceId, ct));
        }
    }
}
