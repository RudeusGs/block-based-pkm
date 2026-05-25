using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;
using Pkm.Application.Features.Social.Services;

namespace Pkm.Application.Features.Social.Commands;

public sealed record UpdateMyProfilePageCommand(string? Bio, string? CoverImageUrl)
    : ICommand<UserProfilePageDto>;

public sealed class UpdateMyProfilePageHandler
    : ICommandHandler<UpdateMyProfilePageCommand, UserProfilePageDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public UpdateMyProfilePageHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<UserProfilePageDto>> HandleAsync(
        UpdateMyProfilePageCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.UpdateMyProfilePageAsync(
            command.Bio,
            command.CoverImageUrl,
            cancellationToken);
}

public sealed record UploadMyProfileCoverImageCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<UserProfilePageDto>;

public sealed class UploadMyProfileCoverImageHandler
    : ICommandHandler<UploadMyProfileCoverImageCommand, UserProfilePageDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public UploadMyProfileCoverImageHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<UserProfilePageDto>> HandleAsync(
        UploadMyProfileCoverImageCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.UploadMyProfileCoverImageAsync(
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.Content,
            cancellationToken);
}

public sealed record SendFriendRequestCommand(Guid AddresseeUserId)
    : ICommand<FriendRequestDto>;

public sealed class SendFriendRequestHandler
    : ICommandHandler<SendFriendRequestCommand, FriendRequestDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public SendFriendRequestHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<FriendRequestDto>> HandleAsync(
        SendFriendRequestCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.SendFriendRequestAsync(
            command.AddresseeUserId,
            cancellationToken);
}

public sealed record AcceptFriendRequestCommand(Guid RequestId)
    : ICommand<FriendRequestDto>;

public sealed class AcceptFriendRequestHandler
    : ICommandHandler<AcceptFriendRequestCommand, FriendRequestDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public AcceptFriendRequestHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<FriendRequestDto>> HandleAsync(
        AcceptFriendRequestCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.AcceptFriendRequestAsync(
            command.RequestId,
            cancellationToken);
}

public sealed record RejectFriendRequestCommand(Guid RequestId)
    : ICommand<FriendRequestDto>;

public sealed class RejectFriendRequestHandler
    : ICommandHandler<RejectFriendRequestCommand, FriendRequestDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public RejectFriendRequestHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<FriendRequestDto>> HandleAsync(
        RejectFriendRequestCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.RejectFriendRequestAsync(
            command.RequestId,
            cancellationToken);
}

public sealed record CancelFriendRequestCommand(Guid RequestId)
    : ICommand<FriendRequestDto>;

public sealed class CancelFriendRequestHandler
    : ICommandHandler<CancelFriendRequestCommand, FriendRequestDto>
{
    private readonly ISocialCommandService _socialCommandService;

    public CancelFriendRequestHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result<FriendRequestDto>> HandleAsync(
        CancelFriendRequestCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.CancelFriendRequestAsync(
            command.RequestId,
            cancellationToken);
}

public sealed record RemoveFriendCommand(Guid FriendUserId)
    : ICommand;

public sealed class RemoveFriendHandler : ICommandHandler<RemoveFriendCommand>
{
    private readonly ISocialCommandService _socialCommandService;

    public RemoveFriendHandler(ISocialCommandService socialCommandService)
    {
        _socialCommandService = socialCommandService;
    }

    public Task<Result> HandleAsync(
        RemoveFriendCommand command,
        CancellationToken cancellationToken = default)
        => _socialCommandService.RemoveFriendAsync(
            command.FriendUserId,
            cancellationToken);
}
