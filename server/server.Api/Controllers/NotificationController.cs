using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models;

namespace server.Controllers
{
    [Route("api/notifications")]
    [Authorize]
    [ApiController]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int? workspaceId, [FromQuery] PagingRequest? paging, CancellationToken ct)
        {
            return FromApiResult(await _notificationService.GetNotificationsAsync(workspaceId, paging, ct));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] int? workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _notificationService.GetUnreadCountAsync(workspaceId, ct));
        }

        [HttpPut("{notificationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId, CancellationToken ct)
        {
            return FromApiResult(await _notificationService.MarkAsReadAsync(notificationId, ct));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] int? workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _notificationService.MarkAllAsReadAsync(workspaceId, ct));
        }
    }
}
