using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Account;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Account;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Account.Commands.ChangeMyPassword;
using Pkm.Application.Features.Account.Commands.UpdateMyProfile;
using Pkm.Application.Features.Account.Queries.GetMyProfile;
using Pkm.Application.Features.Authentication;
using Pkm.Application.Features.Authentication.Queries.GetUserRoles;
using Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;
using Pkm.Api.Contracts.Responses.Tasks;
using Pkm.Application.Features.Tasks;
using Pkm.Application.Features.Tasks.Queries.ListMyAssignedTasks;
using Pkm.Domain.Tasks;
namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/me")]
public sealed class MeController : BaseController
{
    private readonly GetMyProfileHandler _getMyProfileHandler;
    private readonly UpdateMyProfileHandler _updateMyProfileHandler;
    private readonly ChangeMyPasswordHandler _changeMyPasswordHandler;
    private readonly GetUserRolesHandler _getUserRolesHandler;
    private readonly ListMyWorkspacesHandler _listMyWorkspacesHandler;
    private readonly ListMyAssignedTasksHandler _listMyAssignedTasksHandler;
    public MeController(
        ICurrentUser currentUser,
        GetMyProfileHandler getMyProfileHandler,
        UpdateMyProfileHandler updateMyProfileHandler,
        ChangeMyPasswordHandler changeMyPasswordHandler,
        GetUserRolesHandler getUserRolesHandler,
        ListMyWorkspacesHandler listMyWorkspacesHandler,
        ListMyAssignedTasksHandler listMyAssignedTasksHandler)
        : base(currentUser)
    {
        _getMyProfileHandler = getMyProfileHandler;
        _updateMyProfileHandler = updateMyProfileHandler;
        _changeMyPasswordHandler = changeMyPasswordHandler;
        _getUserRolesHandler = getUserRolesHandler;
        _listMyWorkspacesHandler = listMyWorkspacesHandler;
        _listMyAssignedTasksHandler = listMyAssignedTasksHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<UserProfileResponse>>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var result = await _getMyProfileHandler.HandleAsync(
            new GetMyProfileQuery(),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("profile")]
    [ProducesResponseType(typeof(ApiResult<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<UserProfileResponse>>> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateMyProfileHandler.HandleAsync(
            new UpdateMyProfileCommand(
                request.FullName,
                request.AvatarUrl),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("password")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult>> ChangeMyPassword(
        [FromBody] ChangeMyPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _changeMyPasswordHandler.HandleAsync(
            new ChangeMyPasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                GetClientIpAddress()),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<string>>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<IEnumerable<string>>>> GetMyRoles(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return HandleResult<IEnumerable<string>>(
                Result.Failure<IEnumerable<string>>(
                    AuthenticationErrors.MissingUserContext));
        }

        var result = await _getUserRolesHandler.HandleAsync(
            new GetUserRolesQuery(currentUserId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("workspaces")]
    [ProducesResponseType(typeof(ApiResult<WorkspacePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<WorkspacePagedResultResponse>>> ListMyWorkspaces(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _listMyWorkspacesHandler.HandleAsync(
            new ListMyWorkspacesQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(ApiResult<WorkTaskPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<WorkTaskPagedResultResponse>>> ListMyAssignedTasks(
    [FromQuery] Guid? workspaceId = null,
    [FromQuery] string? keyword = null,
    [FromQuery] string? status = null,
    [FromQuery] string? priority = null,
    [FromQuery] DateTimeOffset? dueFrom = null,
    [FromQuery] DateTimeOffset? dueTo = null,
    [FromQuery] bool includeCompleted = true,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        if (!TryParseNullableStatus(status, out var parsedStatus))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                    "Status không hợp lệ. Giá trị hợp lệ: todo, doing, done."
                    })));
        }

        if (!TryParseNullablePriority(priority, out var parsedPriority))
        {
            return HandleResult<WorkTaskPagedResultResponse>(
                Result.Failure<WorkTaskPagedResultResponse>(
                    TaskErrors.InvalidListRequest(new[]
                    {
                    "Priority không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var result = await _listMyAssignedTasksHandler.HandleAsync(
            new ListMyAssignedTasksQuery(
                workspaceId,
                keyword,
                parsedStatus,
                parsedPriority,
                dueFrom,
                dueTo,
                includeCompleted,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
    private string? GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
    private static bool TryParsePriority(string? raw, out PriorityWorkTask priority)
    {
        priority = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "low":
                priority = PriorityWorkTask.Low;
                return true;
            case "medium":
                priority = PriorityWorkTask.Medium;
                return true;
            case "high":
                priority = PriorityWorkTask.High;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseNullablePriority(string? raw, out PriorityWorkTask? priority)
    {
        priority = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (TryParsePriority(raw, out var parsed))
        {
            priority = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseStatus(string? raw, out StatusWorkTask status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim().ToLowerInvariant().Replace("-", "_"))
        {
            case "todo":
            case "to_do":
                status = StatusWorkTask.ToDo;
                return true;
            case "doing":
                status = StatusWorkTask.Doing;
                return true;
            case "done":
                status = StatusWorkTask.Done;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseNullableStatus(string? raw, out StatusWorkTask? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (TryParseStatus(raw, out var parsed))
        {
            status = parsed;
            return true;
        }

        return false;
    }
}
