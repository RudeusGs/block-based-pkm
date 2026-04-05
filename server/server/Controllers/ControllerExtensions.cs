using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace server.Controllers
{
    public static class ControllerExtensions
    {
        public static bool TryGetUserId(this ControllerBase controller, out int userId)
        {
            userId = 0;
            var claim = controller?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim)) return false;
            return int.TryParse(claim, out userId);
        }

        public static IActionResult FailUnauthorized(this ControllerBase controller)
        {
            var api = server.Service.Models.ApiResult.Fail("Unauthorized", "UNAUTHORIZED");
            return new ObjectResult(api) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}
