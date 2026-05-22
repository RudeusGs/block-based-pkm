using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses.ActivityLogs;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Activity.Queries.ListWorkspaceActivityLogs;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/activity-logs")]
public sealed class ActivityLogsController : BaseController
{
    private readonly ListWorkspaceActivityLogsHandler _listWorkspaceActivityLogsHandler;

    public ActivityLogsController(
        ICurrentUser currentUser,
        ListWorkspaceActivityLogsHandler listWorkspaceActivityLogsHandler)
        : base(currentUser)
    {
        _listWorkspaceActivityLogsHandler = listWorkspaceActivityLogsHandler;
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
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _listWorkspaceActivityLogsHandler.HandleAsync(
            new ListWorkspaceActivityLogsQuery(
                workspaceId,
                action,
                entityType,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
}
