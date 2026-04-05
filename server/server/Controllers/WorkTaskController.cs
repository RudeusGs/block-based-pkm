using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain.Enums;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.WorkTask;

namespace server.Controllers
{
    [Route("api/work-tasks")]
    [Authorize]
    [ApiController]
    public class WorkTaskController : BaseController
    {
        private readonly IWorkTaskService _taskService;

        public WorkTaskController(IWorkTaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddWorkTaskModel model, CancellationToken ct)
        {
            var result = await _taskService.CreateTaskAsync(model, ct);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{taskId:int}")]
        public async Task<IActionResult> Update([FromBody] UpdateWorkTaskModel model, CancellationToken ct)
        {
            var result = await _taskService.UpdateTaskAsync(model, ct);
            return FromApiResult(result);
        }

        [HttpDelete("{taskId:int}")]
        public async Task<IActionResult> Delete(int taskId, CancellationToken ct)
        {
            var result = await _taskService.DeleteTaskAsync(taskId, ct);
            return FromApiResult(result);
        }

        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetById(int taskId, CancellationToken ct)
        {
            var result = await _taskService.GetTaskByIdAsync(taskId, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetByWorkspace(int workspaceId, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _taskService.GetTasksByWorkspaceAsync(workspaceId, paging, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}/search")]
        public async Task<IActionResult> Search(int workspaceId, [FromQuery] string? q, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _taskService.SearchTasksAsync(workspaceId, q ?? string.Empty, paging, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}/status")]
        public async Task<IActionResult> GetByStatus(int workspaceId, [FromQuery] StatusWorkTask status, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _taskService.GetTasksByStatusAsync(workspaceId, status, paging, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}/overdue")]
        public async Task<IActionResult> GetOverdue(int workspaceId, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _taskService.GetOverdueTasksAsync(workspaceId, paging, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}/recommended")]
        public async Task<IActionResult> GetRecommended(int workspaceId, [FromQuery] int limit = 5, CancellationToken ct = default)
        {
            var result = await _taskService.GetRecommendedTasksAsync(workspaceId, limit, ct);
            return FromApiResult(result);
        }

        [HttpPost("{taskId:int}/complete")]
        public async Task<IActionResult> Complete(int taskId, [FromQuery] int durationMinutes, CancellationToken ct)
        {
            var result = await _taskService.CompleteTaskAsync(taskId, durationMinutes, ct);
            return FromApiResult(result);
        }

        [HttpPost("{taskId:int}/reopen")]
        public async Task<IActionResult> ReOpen(int taskId, CancellationToken ct)
        {
            var result = await _taskService.ReOpenTaskAsync(taskId, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}/statistics")]
        public async Task<IActionResult> GetStatistics(int workspaceId, CancellationToken ct)
        {
            var result = await _taskService.GetTaskStatisticsAsync(workspaceId, ct);
            return FromApiResult(result);
        }
    }
}