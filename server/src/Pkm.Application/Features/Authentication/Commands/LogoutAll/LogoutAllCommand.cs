namespace Pkm.Application.Features.Authentication.Commands.LogoutAll;

public sealed record LogoutAllCommand(
    string? IpAddress = null);
