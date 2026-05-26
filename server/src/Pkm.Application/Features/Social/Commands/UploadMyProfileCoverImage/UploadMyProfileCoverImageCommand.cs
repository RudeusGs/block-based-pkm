using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Files.Models;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Profiles;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Social.Commands;

public sealed record UploadMyProfileCoverImageCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<UserProfilePageDto>;

public sealed class UploadMyProfileCoverImageHandler
    : ICommandHandler<UploadMyProfileCoverImageCommand, UserProfilePageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;

    public UploadMyProfileCoverImageHandler(
        ICurrentUser currentUser,
        IUserProfileRepository profileRepository,
        IWorkspaceRepository workspaceRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IFileUploadApplicationService fileUploadApplicationService)
    {
        _currentUser = currentUser;
        _profileRepository = profileRepository;
        _workspaceRepository = workspaceRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _fileUploadApplicationService = fileUploadApplicationService;
    }

    public async Task<Result<UserProfilePageDto>> HandleAsync(
        UploadMyProfileCoverImageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                command.FileName,
                command.ContentType,
                command.SizeBytes,
                command.Content,
                "profile-cover"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<UserProfilePageDto>(uploadResult.Error);

        var profile = await EnsureProfileForUpdateAsync(currentUserId, cancellationToken);

        try
        {
            profile.SetCoverImage(uploadResult.Value.PublicUrl, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfilePageDto>(
                new Error("Social.UploadProfileCoverFailed", ex.Message, ResultStatus.Unprocessable));
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
