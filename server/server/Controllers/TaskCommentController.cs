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
        public async Task<IActionResult> Add([FromBody] AddCommentModel model, CancellationToken ct)
        {
            var result = await _service.AddCommentAsync(model, ct);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{commentId:int}")]
        public async Task<IActionResult> Update(int commentId, [FromBody] UpdateCommentModel model, CancellationToken ct)
        {
            var result = await _service.UpdateCommentAsync(commentId, model, ct);
            return FromApiResult(result);
        }

        [HttpDelete("{commentId:int}")]
        public async Task<IActionResult> Delete(int commentId, CancellationToken ct)
        {
            var result = await _service.SoftDeleteCommentAsync(commentId, ct);
            return FromApiResult(result);
        }

        [HttpGet("{commentId:int}")]
        public async Task<IActionResult> GetById(int commentId, CancellationToken ct)
        {
            var result = await _service.GetCommentByIdAsync(commentId, ct);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}")]
        public async Task<IActionResult> GetByTask(int taskId, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _service.GetTaskCommentsAsync(taskId, paging, ct);
            return FromApiResult(result);
        }

        [HttpGet("tasks/{taskId:int}/count")]
        public async Task<IActionResult> Count(int taskId, CancellationToken ct)
        {
            var result = await _service.GetCommentCountAsync(taskId, ct);
            return FromApiResult(result);
        }

        [HttpGet("users/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            var result = await _service.GetCommentsByUserAsync(userId, paging, ct);
            return FromApiResult(result);
        }

        [HttpPut("{commentId:int}/restore")]
        public async Task<IActionResult> Restore(int commentId, CancellationToken ct)
        {
            var result = await _service.RestoreCommentAsync(commentId, ct);
            return FromApiResult(result);
        }

        [HttpGet("{commentId:int}/replies")]
        public async Task<IActionResult> GetReplies(int commentId, CancellationToken ct)
        {
            var result = await _service.GetRepliesAsync(commentId, ct);
            return FromApiResult(result);
        }
    }
}