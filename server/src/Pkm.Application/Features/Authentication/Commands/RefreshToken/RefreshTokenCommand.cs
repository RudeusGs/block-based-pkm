using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Authentication.Models;

namespace Pkm.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : ICommand<AuthTokenDto>;
