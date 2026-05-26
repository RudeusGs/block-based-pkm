using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Social.Commands;

public sealed record RemoveFriendCommand(Guid FriendUserId)
    : ICommand;
