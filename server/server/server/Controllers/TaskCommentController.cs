using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.TaskComment;

namespace server.Controllers
{
    [Route("api/task-comment")]
    [Authorize]

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
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return FailResult("Unauthorized", StatusCodes.Status401Unauthorized, "UNAUTHORIZED");

            var result = await _service.AddCommentAsync(model, userId);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateCommentModel model)
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

        [HttpGet("task/{taskId:int}")]
        public async Task<IActionResult> GetByTask(int taskId)
        {
            var result = await _service.GetTaskCommentsAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("count/{taskId:int}")]
        public async Task<IActionResult> Count(int taskId)
        {
            var result = await _service.GetCommentCountAsync(taskId);
            return FromApiResult(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var result = await _service.GetCommentsByUserAsync(userId);
            return FromApiResult(result);
        }
    }
}
