using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain.Enums;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Controllers
{
    [Route("api/work-task")]
    [Authorize]
    public class WorkTaskController : BaseController
    {
        private readonly IWorkTaskService _taskService;

        public WorkTaskController(IWorkTaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddWorkTaskModel model)
        {
            if (!this.TryGetUserId(out var userId))
                return this.FailUnauthorized();

            var result = await _taskService.CreateTaskAsync(model, userId);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateWorkTaskModel model)
        {
            var result = await _taskService.UpdateTaskAsync(model);
            return FromApiResult(result);
        }

        [HttpPut("{taskId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int taskId, [FromBody] StatusWorkTask newStatus)
        {
            var result = await _taskService.UpdateTaskStatusAsync(taskId, newStatus);
            return FromApiResult(result);
        }

        [HttpDelete("{taskId:int}")]
        public async Task<IActionResult> Delete(int taskId)
        {
            var result = await _taskService.DeleteTaskAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetById(int taskId, CancellationToken ct)
        {
            var result = await _taskService.GetTaskByIdAsync(taskId, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetByWorkspace(int workspaceId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksByWorkspaceAsync(workspaceId, paging);
            return FromApiResult(result);
        }

        [HttpGet("page/{pageId:int}")]
        public async Task<IActionResult> GetByPage(int pageId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksByPageAsync(pageId, paging);
            return FromApiResult(result);
        }

        [HttpGet("search/{workspaceId:int}")]
        public async Task<IActionResult> Search(int workspaceId, [FromQuery] string? q, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.SearchTasksAsync(workspaceId, q ?? string.Empty, paging);
            return FromApiResult(result);
        }

        [HttpGet("status/{workspaceId:int}")]
        public async Task<IActionResult> GetByStatus(int workspaceId, [FromQuery] StatusWorkTask status, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksByStatusAsync(workspaceId, status, paging);
            return FromApiResult(result);
        }

        [HttpGet("overdue/{workspaceId:int}")]
        public async Task<IActionResult> GetOverdue(int workspaceId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetOverdueTasksAsync(workspaceId, paging);
            return FromApiResult(result);
        }

        [HttpGet("upcoming/{workspaceId:int}")]
        public async Task<IActionResult> GetUpcoming(int workspaceId, [FromQuery] PagingRequest? paging, [FromQuery] int days = 7)
        {
            var result = await _taskService.GetUpcomingTasksAsync(workspaceId, days, paging);
            return FromApiResult(result);
        }

        [HttpGet("creator/{workspaceId:int}/{userId:int}")]
        public async Task<IActionResult> GetByCreator(int workspaceId, int userId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksByCreatorAsync(workspaceId, userId, paging);
            return FromApiResult(result);
        }

        [HttpGet("assigned/{workspaceId:int}/{userId:int}")]
        public async Task<IActionResult> GetAssigned(int workspaceId, int userId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetAssignedTasksAsync(workspaceId, userId, paging);
            return FromApiResult(result);
        }

        [HttpGet("sort/priority/{workspaceId:int}")]
        public async Task<IActionResult> GetSortedByPriority(int workspaceId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksSortedByPriorityAsync(workspaceId, paging);
            return FromApiResult(result);
        }

        [HttpGet("sort/duedate/{workspaceId:int}")]
        public async Task<IActionResult> GetSortedByDueDate(int workspaceId, [FromQuery] PagingRequest? paging)
        {
            var result = await _taskService.GetTasksSortedByDueDateAsync(workspaceId, paging);
            return FromApiResult(result);
        }
    }
}
