namespace Pkm.Application.Features.Authentication.Models;

public sealed record AuthTokenDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthUserDto User);
