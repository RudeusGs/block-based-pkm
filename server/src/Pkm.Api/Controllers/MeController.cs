using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Mapping;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Account;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Account;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Commands.ChangeMyPassword;
using Pkm.Application.Features.Account.Models;
using Pkm.Application.Features.Account.Commands.UpdateMyProfile;
using Pkm.Application.Features.Account.Queries.GetMyProfile;
using Pkm.Application.Features.Authentication;
using Pkm.Application.Features.Authentication.Queries.GetUserRoles;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;
using Pkm.Api.Contracts.Responses.Tasks;
using Pkm.Application.Features.Tasks;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Queries.ListMyAssignedTasks;
namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/me")]
public sealed class MeController : BaseController
{
    private readonly IUseCaseDispatcher _dispatcher;

    public MeController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<UserProfileResponse>>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync<GetMyProfileQuery, UserProfileDto>(
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
        var result = await _dispatcher.ExecuteAsync<UpdateMyProfileCommand, UserProfileDto>(
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
        var result = await _dispatcher.ExecuteAsync(
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

        var result = await _dispatcher.QueryAsync<GetUserRolesQuery, IEnumerable<string>>(
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
        var result = await _dispatcher.QueryAsync<ListMyWorkspacesQuery, WorkspacePagedResultDto>(
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

        var result = await _dispatcher.QueryAsync<ListMyAssignedTasksQuery, WorkTaskPagedResultDto>(
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
}
