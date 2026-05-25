using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Authentication.Models;

namespace Pkm.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string? AvatarUrl) : ICommand<AuthUserDto>;
