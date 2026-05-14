namespace Pkm.Application.Features.Account.Commands.ChangeMyPassword;

public sealed record ChangeMyPasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string? IpAddress = null);