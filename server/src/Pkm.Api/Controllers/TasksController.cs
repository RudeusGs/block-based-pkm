using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Tasks;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Tasks;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Parsing;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks;
using Pkm.Application.Features.Tasks.Commands.AssignTask;
using Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;
using Pkm.Application.Features.Tasks.Commands.CreateWorkTask;
using Pkm.Application.Features.Tasks.Commands.DeleteWorkTask;
using Pkm.Application.Features.Tasks.Commands.UnassignTask;
using Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Queries.GetWorkTaskById;
using Pkm.Application.Features.Tasks.Queries.ListPageTasks;
using Pkm.Application.Features.Tasks.Queries.ListWorkspaceTasks;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class TasksController : BaseController
{
    public TasksController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser, dispatcher)
    {
    }

    [HttpPost("api/v1/pages/{pageId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> Create(
        [FromRoute] Guid pageId,
        [FromBody] CreateWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!EnumRequestParsers.TryParseTaskPriority(request.Priority, out var priority))
        {
            return HandleResult<WorkTaskResponse>(
                Result.Failure<WorkTaskResponse>(
                    TaskErrors.InvalidCreateRequest(new[]
                    {
                        "Priority không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var command = new CreateWorkTaskCommand(
            pageId,
            request.Title,
            request.Description,
            priority,
            request.DueDate,
            request.AssigneeUserIds);

        var result = await ExecuteAsync<CreateWorkTaskCommand, WorkTaskDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("api/v1/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> Update(
        [FromRoute] Guid taskId,
        [FromBody] UpdateWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!EnumRequestParsers.TryParseTaskPriority(request.Priority, out var priority))
        {
            return HandleResult<WorkTaskResponse>(
                Result.Failure<WorkTaskResponse>(
                    TaskErrors.InvalidUpdateRequest(new[]
                    {
                        "Priority không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var command = new UpdateWorkTaskCommand(
            taskId,
            request.PageId,
            request.Title,
            request.Description,
            priority,
            request.DueDate);

        var result = await ExecuteAsync<UpdateWorkTaskCommand, WorkTaskDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Delete(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            new DeleteWorkTaskCommand(taskId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("api/v1/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> GetById(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await QueryAsync<GetWorkTaskByIdQuery, WorkTaskDto>(
            new GetWorkTaskByIdQuery(taskId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkTaskPagedResultResponse>>> GetByPageId(
        [FromRoute] Guid pageId,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] Guid? assigneeUserId = null,
        [FromQuery] DateTimeOffset? dueFrom = null,
        [FromQuery] DateTimeOffset? dueTo = null,
        [FromQuery] bool includeCompleted = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!EnumRequestParsers.TryParseNullableTaskStatus(status, out var parsedStatus))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                        "Status không hợp lệ. Giá trị hợp lệ: todo, doing, done."
                    })));
        }

        if (!EnumRequestParsers.TryParseNullableTaskPriority(priority, out var parsedPriority))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                        "Priority không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var result = await QueryAsync<ListPageTasksQuery, WorkTaskPagedResultDto>(
            new ListPageTasksQuery(
                pageId,
                keyword,
                parsedStatus,
                parsedPriority,
                assigneeUserId,
                dueFrom,
                dueTo,
                includeCompleted,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkTaskPagedResultResponse>>> GetByWorkspaceId(
        [FromRoute] Guid workspaceId,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] Guid? assigneeUserId = null,
        [FromQuery] DateTimeOffset? dueFrom = null,
        [FromQuery] DateTimeOffset? dueTo = null,
        [FromQuery] bool includeCompleted = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!EnumRequestParsers.TryParseNullableTaskStatus(status, out var parsedStatus))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                        "Status không hợp lệ. Giá trị hợp lệ: todo, doing, done."
                    })));
        }

        if (!EnumRequestParsers.TryParseNullableTaskPriority(priority, out var parsedPriority))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                        "Priority không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var result = await QueryAsync<ListWorkspaceTasksQuery, WorkTaskPagedResultDto>(
            new ListWorkspaceTasksQuery(
                workspaceId,
                keyword,
                parsedStatus,
                parsedPriority,
                assigneeUserId,
                dueFrom,
                dueTo,
                includeCompleted,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/tasks/{taskId:guid}/assignees")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> Assign(
        [FromRoute] Guid taskId,
        [FromBody] AssignWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<AssignTaskCommand, WorkTaskDto>(
            new AssignTaskCommand(taskId, request.UserId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/tasks/{taskId:guid}/assignees/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> Unassign(
        [FromRoute] Guid taskId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<UnassignTaskCommand, WorkTaskDto>(
            new UnassignTaskCommand(taskId, userId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/tasks/{taskId:guid}:change-status")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<WorkTaskResponse>>> ChangeStatus(
        [FromRoute] Guid taskId,
        [FromBody] ChangeWorkTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!EnumRequestParsers.TryParseTaskStatus(request.Status, out var status))
        {
            return HandleResult<WorkTaskResponse>(
                Result.Failure<WorkTaskResponse>(
                    TaskErrors.InvalidStatusChangeRequest(new[]
                    {
                        "Status không hợp lệ. Giá trị hợp lệ: todo, doing, done."
                    })));
        }

        var result = await ExecuteAsync<ChangeWorkTaskStatusCommand, WorkTaskDto>(
            new ChangeWorkTaskStatusCommand(taskId, status),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
}
