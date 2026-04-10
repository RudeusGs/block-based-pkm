using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;

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
        public async Task<IActionResult> GetNotifications([FromQuery] int? workspaceId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            return FromApiResult(await _notificationService.GetNotificationsAsync(userId, workspaceId, pageIndex, pageSize));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] int? workspaceId)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            return FromApiResult(await _notificationService.GetUnreadCountAsync(userId, workspaceId));
        }

        [HttpPut("{notificationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            return FromApiResult(await _notificationService.MarkAsReadAsync(notificationId, userId));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] int? workspaceId)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            return FromApiResult(await _notificationService.MarkAllAsReadAsync(userId, workspaceId));
        }
    }
}
