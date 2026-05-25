using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;

namespace Pkm.Application.Features.Account.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string FullName,
    string? AvatarUrl) : ICommand<UserProfileDto>;
