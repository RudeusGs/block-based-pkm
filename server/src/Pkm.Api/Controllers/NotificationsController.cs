using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses.Notifications;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Notifications.Commands.DeleteNotification;
using Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Pkm.Application.Features.Notifications.Commands.MarkNotificationAsUnread;
using Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using Pkm.Application.Features.Notifications.Queries.ListNotifications;

namespace Pkm.Api.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : BaseController
{
    private readonly ListNotificationsHandler _listNotificationsHandler;
    private readonly GetUnreadNotificationCountHandler _getUnreadNotificationCountHandler;
    private readonly MarkNotificationAsReadHandler _markNotificationAsReadHandler;
    private readonly MarkNotificationAsUnreadHandler _markNotificationAsUnreadHandler;
    private readonly MarkAllNotificationsAsReadHandler _markAllNotificationsAsReadHandler;
    private readonly DeleteNotificationHandler _deleteNotificationHandler;

    public NotificationsController(
        ICurrentUser currentUser,
        ListNotificationsHandler listNotificationsHandler,
        GetUnreadNotificationCountHandler getUnreadNotificationCountHandler,
        MarkNotificationAsReadHandler markNotificationAsReadHandler,
        MarkNotificationAsUnreadHandler markNotificationAsUnreadHandler,
        MarkAllNotificationsAsReadHandler markAllNotificationsAsReadHandler,
        DeleteNotificationHandler deleteNotificationHandler)
        : base(currentUser)
    {
        _listNotificationsHandler = listNotificationsHandler;
        _getUnreadNotificationCountHandler = getUnreadNotificationCountHandler;
        _markNotificationAsReadHandler = markNotificationAsReadHandler;
        _markNotificationAsUnreadHandler = markNotificationAsUnreadHandler;
        _markAllNotificationsAsReadHandler = markAllNotificationsAsReadHandler;
        _deleteNotificationHandler = deleteNotificationHandler;
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
        var result = await _listNotificationsHandler.HandleAsync(
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
        var result = await _getUnreadNotificationCountHandler.HandleAsync(
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
        var result = await _markNotificationAsReadHandler.HandleAsync(
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
        var result = await _markNotificationAsUnreadHandler.HandleAsync(
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
        var result = await _markAllNotificationsAsReadHandler.HandleAsync(
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
        var result = await _deleteNotificationHandler.HandleAsync(
            new DeleteNotificationCommand(notificationId),
            cancellationToken);

        return HandleResult(result);
    }
}