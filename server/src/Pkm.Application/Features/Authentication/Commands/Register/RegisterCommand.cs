namespace Pkm.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string? AvatarUrl);
