using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Recommendations;
using Pkm.Api.Contracts.Responses.Recommendations;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Recommendations;
using Pkm.Application.Features.Recommendations.Commands.AcceptTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.CompleteTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;
using Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;
using Pkm.Application.Features.Recommendations.Commands.UpdateUserTaskPreference;
using Pkm.Application.Features.Recommendations.Queries.GetUserTaskPreference;
using Pkm.Application.Features.Recommendations.Queries.ListTaskRecommendations;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class RecommendationsController : BaseController
{
    private readonly GenerateTaskRecommendationsHandler _generateHandler;
    private readonly ListTaskRecommendationsHandler _listHandler;
    private readonly AcceptTaskRecommendationHandler _acceptHandler;
    private readonly RejectTaskRecommendationHandler _rejectHandler;
    private readonly CompleteTaskRecommendationHandler _completeHandler;
    private readonly GetUserTaskPreferenceHandler _getPreferenceHandler;
    private readonly UpdateUserTaskPreferenceHandler _updatePreferenceHandler;

    public RecommendationsController(
        ICurrentUser currentUser,
        GenerateTaskRecommendationsHandler generateHandler,
        ListTaskRecommendationsHandler listHandler,
        AcceptTaskRecommendationHandler acceptHandler,
        RejectTaskRecommendationHandler rejectHandler,
        CompleteTaskRecommendationHandler completeHandler,
        GetUserTaskPreferenceHandler getPreferenceHandler,
        UpdateUserTaskPreferenceHandler updatePreferenceHandler)
        : base(currentUser)
    {
        _generateHandler = generateHandler;
        _listHandler = listHandler;
        _acceptHandler = acceptHandler;
        _rejectHandler = rejectHandler;
        _completeHandler = completeHandler;
        _getPreferenceHandler = getPreferenceHandler;
        _updatePreferenceHandler = updatePreferenceHandler;
    }

    [HttpPost("api/v1/workspaces/{workspaceId:guid}/task-recommendations:generate")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<TaskRecommendationResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<TaskRecommendationResponse>>>> Generate(
        [FromRoute] Guid workspaceId,
        [FromBody] GenerateTaskRecommendationsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _generateHandler.HandleAsync(
            new GenerateTaskRecommendationsCommand(
                workspaceId,
                request.PageId,
                request.Force),
            cancellationToken);

        return HandleResult(
            result,
            x => (IReadOnlyList<TaskRecommendationResponse>)x.Select(y => y.ToResponse()).ToArray());
    }

    [HttpGet("api/v1/task-recommendations")]
    [ProducesResponseType(typeof(ApiResult<TaskRecommendationPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    public async Task<ActionResult<ApiResult<TaskRecommendationPagedResultResponse>>> List(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseNullableStatus(status, out var parsedStatus))
        {
            return HandleResult<TaskRecommendationPagedResultResponse>(
                Result.Failure<TaskRecommendationPagedResultResponse>(
                    RecommendationErrors.InvalidListRequest(new[]
                    {
                        "Status không hợp lệ. Giá trị hợp lệ: pending, accepted, rejected, completed, expired."
                    })));
        }

        var result = await _listHandler.HandleAsync(
            new ListTaskRecommendationsQuery(
                workspaceId,
                parsedStatus,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/task-recommendations/{recommendationId:guid}:accept")]
    [ProducesResponseType(typeof(ApiResult<TaskRecommendationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskRecommendationResponse>>> Accept(
        [FromRoute] Guid recommendationId,
        CancellationToken cancellationToken)
    {
        var result = await _acceptHandler.HandleAsync(
            new AcceptTaskRecommendationCommand(recommendationId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/task-recommendations/{recommendationId:guid}:reject")]
    [ProducesResponseType(typeof(ApiResult<TaskRecommendationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskRecommendationResponse>>> Reject(
        [FromRoute] Guid recommendationId,
        CancellationToken cancellationToken)
    {
        var result = await _rejectHandler.HandleAsync(
            new RejectTaskRecommendationCommand(recommendationId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/task-recommendations/{recommendationId:guid}:complete")]
    [ProducesResponseType(typeof(ApiResult<TaskRecommendationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<TaskRecommendationResponse>>> Complete(
        [FromRoute] Guid recommendationId,
        [FromBody] CompleteTaskRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _completeHandler.HandleAsync(
            new CompleteTaskRecommendationCommand(
                recommendationId,
                request.Notes),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/task-recommendation-preference")]
    [ProducesResponseType(typeof(ApiResult<UserTaskPreferenceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<UserTaskPreferenceResponse>>> GetPreference(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var result = await _getPreferenceHandler.HandleAsync(
            new GetUserTaskPreferenceQuery(workspaceId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPut("api/v1/workspaces/{workspaceId:guid}/task-recommendation-preference")]
    [ProducesResponseType(typeof(ApiResult<UserTaskPreferenceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<UserTaskPreferenceResponse>>> UpdatePreference(
        [FromRoute] Guid workspaceId,
        [FromBody] UpdateUserTaskPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParsePriority(request.MinPriorityForRecommendation, out var minPriority))
        {
            return HandleResult<UserTaskPreferenceResponse>(
                Result.Failure<UserTaskPreferenceResponse>(
                    RecommendationErrors.InvalidPreferenceRequest(new[]
                    {
                        "MinPriorityForRecommendation không hợp lệ. Giá trị hợp lệ: low, medium, high."
                    })));
        }

        var result = await _updatePreferenceHandler.HandleAsync(
            new UpdateUserTaskPreferenceCommand(
                workspaceId,
                request.WorkDayStartHour,
                request.WorkDayEndHour,
                request.PreferredDaysOfWeek,
                request.MaxRecommendationsPerSession,
                minPriority,
                request.RecommendationSensitivity,
                request.RecommendationIntervalMinutes,
                request.EnableAutoRecommendation),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    private static bool TryParseNullableStatus(
        string? raw,
        out StatusTaskRecommendation? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "pending":
                status = StatusTaskRecommendation.Pending;
                return true;
            case "accepted":
                status = StatusTaskRecommendation.Accepted;
                return true;
            case "rejected":
                status = StatusTaskRecommendation.Rejected;
                return true;
            case "completed":
                status = StatusTaskRecommendation.Completed;
                return true;
            case "expired":
                status = StatusTaskRecommendation.Expired;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParsePriority(
        string? raw,
        out PriorityWorkTask priority)
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
}