using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Commands;

public sealed record SendFriendRequestCommand(Guid AddresseeUserId)
    : ICommand<FriendRequestDto>;
