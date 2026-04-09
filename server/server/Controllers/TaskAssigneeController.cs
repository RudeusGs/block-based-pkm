using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
namespace server.Controllers
{
    [Route("api/task-assignees")]
    [Authorize]
    [ApiController]
    public class TaskAssigneeController : BaseController
    {
        private readonly ITaskAssigneeService _service;

        public TaskAssigneeController(ITaskAssigneeService service)
        {
            _service = service;
        }

        [HttpPost("tasks/{taskId:int}/assignees")]
        public async Task<IActionResult> Assign(int taskId, [FromBody] List<int> userIds)
        {

            var result = await _service.AssignTaskToMultipleUsersAsync(taskId, userIds);
            return FromApiResult(result);
        }

        [HttpPost("tasks/{taskId:int}/assignees/{userId:int}")]
        public async Task<IActionResult> AssignSingle(int taskId, int userId)
        {
            var result = await _service.AssignTaskAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpDelete("tasks/{taskId:int}/assignees/{userId:int}")]
        public async Task<IActionResult> Unassign(int taskId, int userId)
        {

            var result = await _service.UnassignTaskAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpDelete("tasks/{taskId:int}/assignees")]
        public async Task<IActionResult> UnassignAll(int taskId)
        {

            var result = await _service.UnassignTaskFromAllAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}/assignees")]
        public async Task<IActionResult> GetAssignees(int taskId)
        {
            var result = await _service.GetAssigneesAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("users/{userId:int}/tasks")]
        public async Task<IActionResult> GetAssignedTasks(int userId)
        {
            var result = await _service.GetAssignedTasksAsync(userId);
            return FromApiResult(result);
        }

        [HttpGet("workspaces/{workspaceId:int}/users/{userId:int}/tasks")]
        public async Task<IActionResult> GetAssignedByWorkspace(int workspaceId, int userId)
        {
            var result = await _service.GetAssignedTasksByWorkspaceAsync(userId, workspaceId);
            return FromApiResult(result);
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetAssignedByUsers([FromQuery] List<int> userIds)
        {
            var result = await _service.GetAssignedTasksByUsersAsync(userIds);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}/users/{userId:int}")]
        public async Task<IActionResult> IsAssigned(int taskId, int userId)
        {
            var result = await _service.IsAssignedAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpPost("tasks/{taskId:int}/assignees/{oldUserId:int}/reassign")]
        public async Task<IActionResult> Reassign(int taskId, int oldUserId, [FromBody] int newUserId)
        {
            var result = await _service.ReassignTaskAsync(taskId, oldUserId, newUserId);
            return FromApiResult(result);
        }
    }
}