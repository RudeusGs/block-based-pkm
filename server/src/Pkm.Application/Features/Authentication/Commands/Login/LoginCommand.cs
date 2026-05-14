namespace Pkm.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string LoginIdentifier,
    string Password,
    string? IpAddress = null);