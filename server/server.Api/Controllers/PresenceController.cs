using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain.Realtime;

namespace server.Controllers
{
    [Authorize]
    public class PresenceController : BaseController
    {
        private readonly IPresenceService _presenceService;

        public PresenceController(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        [HttpPost("heartbeat/{pageId}")]
        public async Task<IActionResult> Heartbeat(int pageId, CancellationToken ct)
        {
            if (CurrentUserId == null) return Unauthorized();

            await _presenceService.HeartbeatPageAsync(pageId, CurrentUserId.Value, CurrentUserName, ct);
            return OkResult(true, "Cập nhật trạng thái hiện diện thành công.");
        }

        [HttpGet("active-users/{pageId}")]
        public async Task<IActionResult> GetActiveUsers(int pageId, CancellationToken ct)
        {
            var users = await _presenceService.GetActiveUsersOnPageAsync(pageId, ct);
            return OkResult(users, "Lấy danh sách active user thành công.");
        }

        [HttpPost("lock-block/{blockId}")]
        public async Task<IActionResult> AcquireLock(int blockId, CancellationToken ct)
        {
            if (CurrentUserId == null) return Unauthorized();

            bool locked = await _presenceService.AcquireBlockLockAsync(blockId, CurrentUserId.Value, ct);
            if (locked)
            {
                return OkResult(new { blockId }, "Khóa block thành công.");
            }

            return FailResult("Block hiện đang bị khóa bởi người khác hoặc thao tác thất bại.", 409);
        }

        [HttpPost("unlock-block/{blockId}")]
        public async Task<IActionResult> ReleaseLock(int blockId, CancellationToken ct)
        {
            if (CurrentUserId == null) return Unauthorized();

            await _presenceService.ReleaseBlockLockAsync(blockId, CurrentUserId.Value, ct);
            return OkResult(true, "Nhả khóa block thành công.");
        }
    }
}
