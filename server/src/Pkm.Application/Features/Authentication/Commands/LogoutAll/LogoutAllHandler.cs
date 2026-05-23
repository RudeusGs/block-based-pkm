using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Authentication.Commands.LogoutAll;

public sealed class LogoutAllHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutAllHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(
        LogoutAllCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(AuthenticationErrors.MissingUserContext);
        }

        var now = _clock.UtcNow;

        var user = await _userRepository.GetByIdForUpdateAsync(
            currentUserId,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthenticationErrors.UserNotFound);
        }

        var activeTokens = await _refreshTokenRepository.ListActiveByUserForUpdateAsync(
            currentUserId,
            now,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, request.IpAddress);
            _refreshTokenRepository.Update(token);
        }

        user.SetAuthenticated(false, now);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
