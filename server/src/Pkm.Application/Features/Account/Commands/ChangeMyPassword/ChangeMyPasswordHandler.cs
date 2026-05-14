using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Authentication;
using Pkm.Domain.Common;
using Pkm.Domain.Users;

namespace Pkm.Application.Features.Account.Commands.ChangeMyPassword;

public sealed class ChangeMyPasswordHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ChangeMyPasswordCommandValidator _validator;

    public ChangeMyPasswordHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        ChangeMyPasswordCommandValidator validator)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(
        ChangeMyPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure(
                AccountErrors.InvalidChangePasswordRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(AuthenticationErrors.MissingUserContext);
        }

        var user = await _userRepository.GetByIdForUpdateAsync(
            currentUserId,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthenticationErrors.UserNotFound);
        }

        if (!user.IsActive())
        {
            return Result.Failure(AuthenticationErrors.UserInactive);
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(AccountErrors.InvalidCurrentPassword);
        }

        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            return Result.Failure(AccountErrors.NewPasswordSameAsCurrent);
        }

        var now = _clock.UtcNow;

        try
        {
            user.ChangePassword(request.NewPassword, _passwordHasher, now);
            user.SetAuthenticated(false, now);

            _userRepository.Update(user);

            var activeTokens = await _refreshTokenRepository.ListActiveByUserForUpdateAsync(
                currentUserId,
                now,
                cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke(now, request.IpAddress);
                _refreshTokenRepository.Update(token);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(new Error(
                AccountErrors.InvalidProfileData.Code,
                ex.Message,
                AccountErrors.InvalidProfileData.Status));
        }
    }
}