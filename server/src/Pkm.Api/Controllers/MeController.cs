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

    public MeController(
        ICurrentUser currentUser,
        GetMyProfileHandler getMyProfileHandler,
        UpdateMyProfileHandler updateMyProfileHandler,
        ChangeMyPasswordHandler changeMyPasswordHandler,
        GetUserRolesHandler getUserRolesHandler,
        ListMyWorkspacesHandler listMyWorkspacesHandler)
        : base(currentUser)
    {
        _getMyProfileHandler = getMyProfileHandler;
        _updateMyProfileHandler = updateMyProfileHandler;
        _changeMyPasswordHandler = changeMyPasswordHandler;
        _getUserRolesHandler = getUserRolesHandler;
        _listMyWorkspacesHandler = listMyWorkspacesHandler;
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

    private string? GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
}