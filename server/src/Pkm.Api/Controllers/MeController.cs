using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Workspaces;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Authentication;
using Pkm.Application.Features.Authentication.Queries.GetUserRoles;
using Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/me")]
public sealed class MeController : BaseController
{
    private readonly GetUserRolesHandler _getUserRolesHandler;
    private readonly ListMyWorkspacesHandler _listMyWorkspacesHandler;

    public MeController(
        ICurrentUser currentUser,
        GetUserRolesHandler getUserRolesHandler,
        ListMyWorkspacesHandler listMyWorkspacesHandler)
        : base(currentUser)
    {
        _getUserRolesHandler = getUserRolesHandler;
        _listMyWorkspacesHandler = listMyWorkspacesHandler;
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
                Result.Failure<IEnumerable<string>>(AuthenticationErrors.MissingUserContext));
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
}