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
        public async Task<IActionResult> GetUserTaskHistory(int userId)
        {
            return FromApiResult(await _historyService.GetUserTaskHistoryAsync(userId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetUserTaskHistoryByWorkspace(int userId, int workspaceId)
        {
            return FromApiResult(await _historyService.GetUserTaskHistoryByWorkspaceAsync(userId, workspaceId));
        }

        [HttpGet("task/{taskId:int}")]
        public async Task<IActionResult> GetTaskHistory(int taskId)
        {
            return FromApiResult(await _historyService.GetTaskHistoryAsync(taskId));
        }

        [HttpPost("date-range")]
        public async Task<IActionResult> GetHistoryByDateRange([FromBody] GetHistoryDateRangeModel model)
        {
            return FromApiResult(await _historyService.GetHistoryByDateRangeAsync(model));
        }

        [HttpGet("task/{taskId:int}/average-duration")]
        public async Task<IActionResult> GetAverageDuration(int taskId)
        {
            return FromApiResult(await _historyService.GetAverageDurationAsync(taskId));
        }

        [HttpGet("user/{userId:int}/average-duration")]
        public async Task<IActionResult> GetUserAverageDuration(int userId)
        {
            return FromApiResult(await _historyService.GetUserAverageDurationAsync(userId));
        }

        [HttpGet("task/{taskId:int}/completion-count")]
        public async Task<IActionResult> GetCompletionCount(int taskId)
        {
            return FromApiResult(await _historyService.GetCompletionCountAsync(taskId));
        }

        [HttpGet("task/{taskId:int}/completion-rate")]
        public async Task<IActionResult> GetCompletionRate(int taskId)
        {
            return FromApiResult(await _historyService.GetCompletionRateAsync(taskId));
        }

        [HttpDelete("{historyId:int}")]
        public async Task<IActionResult> DeleteHistory(int historyId)
        {
            return FromApiResult(await _historyService.DeleteHistoryAsync(historyId));
        }

        [HttpGet("user/{userId:int}/recent")]
        public async Task<IActionResult> GetRecentCompletions(int userId, [FromQuery] int limit = 10)
        {
            return FromApiResult(await _historyService.GetRecentCompletionsAsync(userId, limit));
        }
    }
}
