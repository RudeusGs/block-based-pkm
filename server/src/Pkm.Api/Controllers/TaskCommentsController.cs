using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Tasks;
using Pkm.Api.Contracts.Responses.Tasks;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Commands.CreateTaskComment;
using Pkm.Application.Features.Tasks.Commands.DeleteTaskComment;
using Pkm.Application.Features.Tasks.Commands.RestoreTaskComment;
using Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Queries.ListTaskComments;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class TaskCommentsController : BaseController
{
    private readonly IUseCaseDispatcher _dispatcher;

    public TaskCommentsController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("api/v1/tasks/{taskId:guid}/comments")]
    [ProducesResponseType(typeof(ApiResult<TaskCommentPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<TaskCommentPagedResultResponse>>> ListByTask(
        [FromRoute] Guid taskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool includeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.QueryAsync<ListTaskCommentsQuery, TaskCommentPagedResultDto>(
            new ListTaskCommentsQuery(
                taskId,
                pageNumber,
                pageSize,
                includeDeleted),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/tasks/{taskId:guid}/comments")]
    [ProducesResponseType(typeof(ApiResult<TaskCommentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskCommentResponse>>> Create(
        [FromRoute] Guid taskId,
        [FromBody] CreateTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<CreateTaskCommentCommand, TaskCommentDto>(
            new CreateTaskCommentCommand(
                taskId,
                request.Content,
                request.ParentId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("api/v1/task-comments/{commentId:guid}")]
    [ProducesResponseType(typeof(ApiResult<TaskCommentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskCommentResponse>>> Update(
        [FromRoute] Guid commentId,
        [FromBody] UpdateTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<UpdateTaskCommentCommand, TaskCommentDto>(
            new UpdateTaskCommentCommand(
                commentId,
                request.Content),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/task-comments/{commentId:guid}")]
    [ProducesResponseType(typeof(ApiResult<TaskCommentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskCommentResponse>>> Delete(
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<DeleteTaskCommentCommand, TaskCommentDto>(
            new DeleteTaskCommentCommand(commentId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/task-comments/{commentId:guid}:restore")]
    [ProducesResponseType(typeof(ApiResult<TaskCommentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskCommentResponse>>> Restore(
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.ExecuteAsync<RestoreTaskCommentCommand, TaskCommentDto>(
            new RestoreTaskCommentCommand(commentId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
}
