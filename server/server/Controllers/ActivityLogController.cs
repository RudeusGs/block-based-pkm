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
        public async Task<IActionResult> GetWorkspaceActivityLogs(int workspaceId)
        {
            return FromApiResult(await _activityLogService.GetWorkspaceActivityLogsAsync(workspaceId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetUserActivityLogs(int userId, int workspaceId)
        {
            return FromApiResult(await _activityLogService.GetUserActivityLogsAsync(userId, workspaceId));
        }

        [HttpGet("entity/{entityType}/{entityId:int}")]
        public async Task<IActionResult> GetEntityActivityLogs(string entityType, int entityId)
        {
            return FromApiResult(await _activityLogService.GetEntityActivityLogsAsync(entityType, entityId));
        }

        [HttpGet("workspace/{workspaceId:int}/action/{actionStr}")]
        public async Task<IActionResult> GetActivityLogsByAction(int workspaceId, string actionStr)
        {
            return FromApiResult(await _activityLogService.GetActivityLogsByActionAsync(workspaceId, actionStr));
        }

        [HttpPost("date-range")]
        public async Task<IActionResult> GetActivityLogsByDateRange([FromBody] GetActivityLogDateRangeModel model)
        {
            return FromApiResult(await _activityLogService.GetActivityLogsByDateRangeAsync(model));
        }

        [HttpGet("workspace/{workspaceId:int}/recent")]
        public async Task<IActionResult> GetRecentActivityLogs(int workspaceId, [FromQuery] int limit = 10)
        {
            return FromApiResult(await _activityLogService.GetRecentActivityLogsAsync(workspaceId, limit));
        }

        [HttpDelete("{logId:int}")]
        public async Task<IActionResult> DeleteActivityLog(int logId)
        {
            return FromApiResult(await _activityLogService.DeleteActivityLogAsync(logId));
        }

        [HttpDelete("workspace/{workspaceId:int}/old/{daysOld:int}")]
        public async Task<IActionResult> DeleteOldActivityLogs(int workspaceId, int daysOld)
        {
            return FromApiResult(await _activityLogService.DeleteOldActivityLogsAsync(workspaceId, daysOld));
        }

        [HttpGet("workspace/{workspaceId:int}/stats")]
        public async Task<IActionResult> GetActivityStats(int workspaceId)
        {
            return FromApiResult(await _activityLogService.GetActivityStatsAsync(workspaceId));
        }
    }
}
