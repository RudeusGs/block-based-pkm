using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces.Authentication;
using server.Service.Models.Authenticate;

namespace server.Controllers
{
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthenticateController : BaseController
    {
        private readonly IAuthenticateService _authenticateService;

        public AuthenticateController(IAuthenticateService authenticateService)
        {
            _authenticateService = authenticateService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel request, CancellationToken ct)
        {
            var result = await _authenticateService.Register(request, ct);
            return FromApiResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel request, CancellationToken ct)
        {
            var result = await _authenticateService.Login(request, ct);
            return FromApiResult(result);
        }
    }
}
