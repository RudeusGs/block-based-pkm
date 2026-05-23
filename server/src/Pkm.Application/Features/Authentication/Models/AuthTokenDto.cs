namespace Pkm.Application.Features.Authentication.Models;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthUserDto User);
