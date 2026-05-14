namespace Pkm.Application.Features.Account.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string FullName,
    string? AvatarUrl);