using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses.ActivityLogs;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Models;
using Pkm.Application.Features.Activity.Queries.ListWorkspaceActivityLogs;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/activity-logs")]
public sealed class ActivityLogsController : BaseController
{
    private readonly IUseCaseDispatcher _dispatcher;

    public ActivityLogsController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<ActivityLogPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<ActivityLogPagedResultResponse>>> List(
        [FromRoute] Guid workspaceId,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.QueryAsync<ListWorkspaceActivityLogsQuery, ActivityLogPagedResultDto>(
            new ListWorkspaceActivityLogsQuery(
                workspaceId,
                action,
                entityType,
                userId,
                fromUtc,
                toUtc,
                search,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
}
