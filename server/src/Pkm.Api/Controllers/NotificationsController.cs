using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses.Notifications;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Commands.DeleteNotification;
using Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsUnread;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using Pkm.Application.Features.Notifications.Queries.ListNotifications;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : BaseController
{
    public NotificationsController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser, dispatcher)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<NotificationPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<NotificationPagedResultResponse>>> List(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListNotificationsQuery, NotificationPagedResultDto>(
            new ListNotificationsQuery(
                workspaceId,
                unreadOnly,
                pageNumber,
                pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResult<NotificationUnreadCountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<NotificationUnreadCountResponse>>> GetUnreadCount(
        [FromQuery] Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<GetUnreadNotificationCountQuery, NotificationUnreadCountDto>(
            new GetUnreadNotificationCountQuery(workspaceId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("{notificationId:guid}:read")]
    [ProducesResponseType(typeof(ApiResult<NotificationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<NotificationResponse>>> MarkAsRead(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<MarkNotificationAsReadCommand, NotificationDto>(
            new MarkNotificationAsReadCommand(notificationId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("{notificationId:guid}:unread")]
    [ProducesResponseType(typeof(ApiResult<NotificationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<NotificationResponse>>> MarkAsUnread(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<MarkNotificationAsUnreadCommand, NotificationDto>(
            new MarkNotificationAsUnreadCommand(notificationId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(ApiResult<NotificationUnreadCountResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<NotificationUnreadCountResponse>>> MarkAllAsRead(
        [FromQuery] Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync<MarkAllNotificationsAsReadCommand, NotificationUnreadCountDto>(
            new MarkAllNotificationsAsReadCommand(workspaceId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("{notificationId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Delete(
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            new DeleteNotificationCommand(notificationId),
            cancellationToken);

        return HandleResult(result);
    }
}
