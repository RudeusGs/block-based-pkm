using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.UserTaskHistory;

namespace server.Controllers
{
    [Route("api/user-task-histories")]
    [Authorize]
    [ApiController]
    public class UserTaskHistoryController : BaseController
    {
        private readonly IUserTaskHistoryService _historyService;

        public UserTaskHistoryController(IUserTaskHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserTaskHistory(int userId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetUserTaskHistoryAsync(userId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetUserTaskHistoryByWorkspace(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetUserTaskHistoryByWorkspaceAsync(userId, workspaceId, ct));
        }

        [HttpGet("task/{taskId:int}")]
        public async Task<IActionResult> GetTaskHistory(int taskId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetTaskHistoryAsync(taskId, ct));
        }

        [HttpPost("date-range")]
        public async Task<IActionResult> GetHistoryByDateRange([FromBody] GetHistoryDateRangeModel model, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetHistoryByDateRangeAsync(model, ct));
        }

        [HttpGet("task/{taskId:int}/average-duration")]
        public async Task<IActionResult> GetAverageDuration(int taskId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetAverageDurationAsync(taskId, ct));
        }

        [HttpGet("user/{userId:int}/average-duration")]
        public async Task<IActionResult> GetUserAverageDuration(int userId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetUserAverageDurationAsync(userId, ct));
        }

        [HttpGet("task/{taskId:int}/completion-count")]
        public async Task<IActionResult> GetCompletionCount(int taskId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetCompletionCountAsync(taskId, ct));
        }

        [HttpGet("task/{taskId:int}/completion-rate")]
        public async Task<IActionResult> GetCompletionRate(int taskId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.GetCompletionRateAsync(taskId, ct));
        }

        [HttpDelete("{historyId:int}")]
        public async Task<IActionResult> DeleteHistory(int historyId, CancellationToken ct)
        {
            return FromApiResult(await _historyService.DeleteHistoryAsync(historyId, ct));
        }

        [HttpGet("user/{userId:int}/recent")]
        public async Task<IActionResult> GetRecentCompletions(int userId, [FromQuery] int limit = 10, CancellationToken ct = default)
        {
            return FromApiResult(await _historyService.GetRecentCompletionsAsync(userId, limit, ct));
        }
    }
}
