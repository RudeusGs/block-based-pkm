using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Auth;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Auth;
using Pkm.Api.Extensions;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Authentication.Commands.Login;
using Pkm.Application.Features.Authentication.Commands.Logout;
using Pkm.Application.Features.Authentication.Commands.LogoutAll;
using Pkm.Application.Features.Authentication.Commands.RefreshToken;
using Pkm.Application.Features.Authentication.Commands.Register;

namespace Pkm.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController : BaseController
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly LogoutAllHandler _logoutAllHandler;

    public AuthController(
        ICurrentUser currentUser,
        LoginHandler loginHandler,
        RegisterHandler registerHandler,
        RefreshTokenHandler refreshTokenHandler,
        LogoutHandler logoutHandler,
        LogoutAllHandler logoutAllHandler)
        : base(currentUser)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutHandler = logoutHandler;
        _logoutAllHandler = logoutAllHandler;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthTokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    public async Task<ActionResult<ApiResult<AuthTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.UserName,
            request.Password,
            GetClientIpAddress());

        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthUserResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 409)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<AuthUserResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.UserName,
            request.Email,
            request.FullName,
            request.Password,
            request.AvatarUrl);

        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthTokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    public async Task<ActionResult<ApiResult<AuthTokenResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _refreshTokenHandler.HandleAsync(
            new RefreshTokenCommand(
                request.RefreshToken,
                GetClientIpAddress()),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    public async Task<ActionResult<ApiResult>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _logoutHandler.HandleAsync(
            new LogoutCommand(
                request.RefreshToken,
                GetClientIpAddress()),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult>> LogoutAll(
        CancellationToken cancellationToken)
    {
        var result = await _logoutAllHandler.HandleAsync(
            new LogoutAllCommand(GetClientIpAddress()),
            cancellationToken);

        return HandleResult(result);
    }

    private string? GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
}