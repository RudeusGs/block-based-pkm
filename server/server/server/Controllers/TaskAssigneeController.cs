using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Controllers
{
    [Route("api/task-assignee")]
    public class TaskAssigneeController : BaseController
    {
        private readonly ITaskAssigneeService _service;

        public TaskAssigneeController(ITaskAssigneeService service)
        {
            _service = service;
        }

        [HttpPost("{taskId:int}/assign/{userId:int}")]
        public async Task<IActionResult> Assign(int taskId, int userId)
        {
            var result = await _service.AssignTaskAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpPost("{taskId:int}/assign")]
        public async Task<IActionResult> AssignMultiple(int taskId, [FromBody] List<int> userIds)
        {
            var result = await _service.AssignTaskToMultipleUsersAsync(taskId, userIds);
            return FromApiResult(result);
        }

        [HttpDelete("{taskId:int}/unassign/{userId:int}")]
        public async Task<IActionResult> Unassign(int taskId, int userId)
        {
            var result = await _service.UnassignTaskAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpDelete("{taskId:int}/unassign")]
        public async Task<IActionResult> UnassignAll(int taskId)
        {
            var result = await _service.UnassignTaskFromAllAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("{taskId:int}/assignees")]
        public async Task<IActionResult> GetAssignees(int taskId)
        {
            var result = await _service.GetAssigneesAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("assigned/{userId:int}")]
        public async Task<IActionResult> GetAssignedTasks(int userId)
        {
            var result = await _service.GetAssignedTasksAsync(userId);
            return FromApiResult(result);
        }

        [HttpGet("assigned/workspace/{workspaceId:int}/{userId:int}")]
        public async Task<IActionResult> GetAssignedByWorkspace(int workspaceId, int userId)
        {
            var result = await _service.GetAssignedTasksByWorkspaceAsync(userId, workspaceId);
            return FromApiResult(result);
        }

        [HttpGet("assigned/users")]
        public async Task<IActionResult> GetAssignedByUsers([FromQuery] List<int> userIds)
        {
            var result = await _service.GetAssignedTasksByUsersAsync(userIds);
            return FromApiResult(result);
        }

        [HttpGet("{taskId:int}/isassigned/{userId:int}")]
        public async Task<IActionResult> IsAssigned(int taskId, int userId)
        {
            var result = await _service.IsAssignedAsync(taskId, userId);
            return FromApiResult(result);
        }

        [HttpPost("{taskId:int}/reassign/{oldUserId:int}/{newUserId:int}")]
        public async Task<IActionResult> Reassign(int taskId, int oldUserId, int newUserId)
        {
            var result = await _service.ReassignTaskAsync(taskId, oldUserId, newUserId);
            return FromApiResult(result);
        }
    }
}
