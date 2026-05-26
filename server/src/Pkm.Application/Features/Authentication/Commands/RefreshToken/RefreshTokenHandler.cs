using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Authentication.Models;
using DomainRefreshToken = Pkm.Domain.Users.RefreshToken;

namespace Pkm.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleService _userRoleService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RefreshTokenCommandValidator _validator;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IUserRoleService userRoleService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenCommandValidator validator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _userRoleService = userRoleService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.InvalidRefreshRequest(validationErrors));
        }

        var now = _clock.UtcNow;
        var oldTokenHash = _refreshTokenService.Hash(request.RefreshToken);

        var oldToken = await _refreshTokenRepository.GetByTokenHashForUpdateAsync(
            oldTokenHash,
            cancellationToken);

        if (oldToken is null || !oldToken.IsActive(now))
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.InvalidRefreshToken);
        }

        var user = await _userRepository.GetByIdForUpdateAsync(
            oldToken.UserId,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.UserNotFound);
        }

        if (!user.IsActive())
        {
            oldToken.Revoke(now, request.IpAddress);
            _refreshTokenRepository.Update(oldToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.UserInactive);
        }

        var roles = await _userRoleService.GetUserRolesAsync(
            user.Id,
            cancellationToken);

        var newAccessToken = _accessTokenService.GenerateToken(user, roles);
        var newRefreshToken = _refreshTokenService.Create(now);

        oldToken.Revoke(
            now,
            request.IpAddress,
            newRefreshToken.TokenHash);

        _refreshTokenRepository.Update(oldToken);

        _refreshTokenRepository.Add(DomainRefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            newRefreshToken.TokenHash,
            newRefreshToken.ExpiresAtUtc,
            now,
            request.IpAddress));

        user.SetAuthenticated(true, now);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokenDto(
            newAccessToken,
            newRefreshToken.RawToken,
            "Bearer",
            _accessTokenService.GetAccessTokenExpirySeconds(),
            newRefreshToken.ExpiresAtUtc,
            user.ToAuthUserDto(isAuthenticated: true)));
    }
}
