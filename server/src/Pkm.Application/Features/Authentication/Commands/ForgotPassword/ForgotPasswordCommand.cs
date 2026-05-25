using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Authentication.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string? IpAddress) : ICommand;
