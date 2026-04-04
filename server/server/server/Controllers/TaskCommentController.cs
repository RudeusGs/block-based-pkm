using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskComment;

namespace server.Controllers
{
    [Route("api/task-comments")]
    [Authorize]
    [ApiController]
    public class TaskCommentController : BaseController
    {
        private readonly ITaskCommentService _service;

        public TaskCommentController(ITaskCommentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddCommentModel model)
        {
            if (!this.TryGetUserId(out var userId))
                return this.FailUnauthorized();

            var result = await _service.AddCommentAsync(model, userId);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{commentId:int}")]
        public async Task<IActionResult> Update(int commentId, [FromBody] UpdateCommentModel model)
        {
            var result = await _service.UpdateCommentAsync(model);
            return FromApiResult(result);
        }

        [HttpDelete("{commentId:int}")]
        public async Task<IActionResult> Delete(int commentId)
        {
            var result = await _service.DeleteCommentAsync(commentId);
            return FromApiResult(result);
        }

        [HttpGet("{commentId:int}")]
        public async Task<IActionResult> GetById(int commentId)
        {
            var result = await _service.GetCommentByIdAsync(commentId);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}/comments")]
        public async Task<IActionResult> GetByTask(int taskId, [FromQuery] PagingRequest? paging = null)
        {
            var result = await _service.GetTaskCommentsAsync(taskId, paging);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}/comments/count")]
        public async Task<IActionResult> Count(int taskId)
        {
            var result = await _service.GetCommentCountAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("users/{userId:int}/comments")]
        public async Task<IActionResult> GetByUser(int userId, [FromQuery] PagingRequest? paging = null)
        {
            var result = await _service.GetCommentsByUserAsync(userId, paging);
            return FromApiResult(result);
        }
    }
}