using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed class GetProfileHandler
    : IQueryHandler<GetProfileQuery, UserProfilePageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public GetProfileHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IUserProfileRepository profileRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<Result<UserProfilePageDto>> HandleAsync(
        GetProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var target = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (target is null)
            return Result.Failure<UserProfilePageDto>(SocialErrors.UserNotFound);

        var page = PageRequest.Normalize(query.WorkspacePageNumber, query.WorkspacePageSize);
        var includePrivate = query.UserId == currentUserId;

        var workspaces = await _workspaceRepository.ListProfileWorkspacesAsync(
            query.UserId,
            currentUserId,
            includePrivate,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _workspaceRepository.CountProfileWorkspacesAsync(
            query.UserId,
            currentUserId,
            includePrivate,
            cancellationToken);

        var profile = await _profileRepository.GetProfileAsync(
            query.UserId,
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
