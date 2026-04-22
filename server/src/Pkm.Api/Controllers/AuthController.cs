using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Auth;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Auth;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Authentication.Commands.Login;
using Pkm.Application.Features.Authentication.Commands.Register;

namespace Pkm.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController : BaseController
{
    private readonly LoginHandler _loginHandler;
    private readonly RegisterHandler _registerHandler;

    public AuthController(
        ICurrentUser currentUser,
        LoginHandler loginHandler,
        RegisterHandler registerHandler)
        : base(currentUser)
    {
        _loginHandler = loginHandler;
        _registerHandler = registerHandler;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthTokenResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<AuthTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.UserName,
            request.Password);

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
}