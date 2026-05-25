using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Authentication.Models;
using Pkm.Domain.Users;
using DomainRefreshToken = Pkm.Domain.Users.RefreshToken;

namespace Pkm.Application.Features.Authentication.Commands.Login;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthTokenDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRoleService _userRoleService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly LoginCommandValidator _validator;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IUserRoleService userRoleService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        IClock clock,
        LoginCommandValidator validator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _userRoleService = userRoleService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.InvalidLoginRequest(validationErrors));
        }

        var user = await _userRepository.GetByLoginIdentifierAsync(
            request.LoginIdentifier,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.InvalidCredentials);
        }

        if (!user.IsActive())
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.UserInactive);
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokenDto>(
                AuthenticationErrors.InvalidCredentials);
        }

        var now = _clock.UtcNow;

        var roles = await _userRoleService.GetUserRolesAsync(
            user.Id,
            cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshToken = _refreshTokenService.Create(now);

        _refreshTokenRepository.Add(DomainRefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            refreshToken.TokenHash,
            refreshToken.ExpiresAtUtc,
            now,
            request.IpAddress));

        user.SetAuthenticated(true, now);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokenDto(
            accessToken,
            refreshToken.RawToken,
            "Bearer",
            _jwtTokenGenerator.GetAccessTokenExpirySeconds(),
            refreshToken.ExpiresAtUtc,
            user.ToAuthUserDto(isAuthenticated: true)));
    }
}
