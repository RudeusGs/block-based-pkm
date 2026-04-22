namespace Pkm.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string UserName,
    string Password
);
