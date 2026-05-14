namespace Pkm.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null);