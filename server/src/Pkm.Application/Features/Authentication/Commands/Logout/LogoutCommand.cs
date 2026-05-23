namespace Pkm.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress = null);
