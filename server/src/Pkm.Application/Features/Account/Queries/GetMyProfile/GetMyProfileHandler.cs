using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;
using Pkm.Application.Features.Authentication;

namespace Pkm.Application.Features.Account.Queries.GetMyProfile;

public sealed class GetMyProfileHandler : IQueryHandler<GetMyProfileQuery, UserProfileDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;

    public GetMyProfileHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<Result<UserProfileDto>> HandleAsync(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<UserProfileDto>(
                AuthenticationErrors.MissingUserContext);
        }

        var user = await _userRepository.GetByIdAsync(
            currentUserId,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserProfileDto>(
                AuthenticationErrors.UserNotFound);
        }

        return Result.Success(user.ToProfileDto());
    }
}
