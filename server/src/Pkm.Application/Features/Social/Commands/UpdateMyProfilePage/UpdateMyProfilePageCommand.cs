using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Profiles;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Social.Commands;

public sealed record UpdateMyProfilePageCommand(string? Bio, string? CoverImageUrl)
    : ICommand<UserProfilePageDto>;

public sealed class UpdateMyProfilePageHandler
    : ICommandHandler<UpdateMyProfilePageCommand, UserProfilePageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateMyProfilePageHandler(
        ICurrentUser currentUser,
        IUserProfileRepository profileRepository,
        IWorkspaceRepository workspaceRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _profileRepository = profileRepository;
        _workspaceRepository = workspaceRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<UserProfilePageDto>> HandleAsync(
        UpdateMyProfilePageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var profile = await EnsureProfileForUpdateAsync(currentUserId, cancellationToken);

        try
        {
            profile.Update(command.Bio, command.CoverImageUrl, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfilePageDto>(
                new Error("Social.UpdateProfilePageFailed", ex.Message, ResultStatus.Unprocessable));
        }

        return await BuildMyProfileAsync(currentUserId, cancellationToken);
    }

    private async Task<UserProfilePage> EnsureProfileForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdForUpdateAsync(userId, cancellationToken);
        if (profile is not null)
            return profile;

        profile = UserProfilePage.Create(Guid.NewGuid(), userId, _clock.UtcNow);
        _profileRepository.Add(profile);
        return profile;
    }

    private async Task<Result<UserProfilePageDto>> BuildMyProfileAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var page = PageRequest.Normalize(1, 20);

        var workspaces = await _workspaceRepository.ListProfileWorkspacesAsync(
            currentUserId,
            currentUserId,
            includePrivate: true,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _workspaceRepository.CountProfileWorkspacesAsync(
            currentUserId,
            currentUserId,
            includePrivate: true,
            cancellationToken);

        var profile = await _profileRepository.GetProfileAsync(
            currentUserId,
            currentUserId,
            workspaces,
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize),
            cancellationToken);

        return profile is null
            ? Result.Failure<UserProfilePageDto>(SocialErrors.UserNotFound)
            : Result.Success(profile);
    }
}
