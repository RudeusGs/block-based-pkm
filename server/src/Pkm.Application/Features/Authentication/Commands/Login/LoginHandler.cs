using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Authentication.Models;
using Pkm.Domain.Users;

namespace Pkm.Application.Features.Authentication.Commands.Login;

public sealed class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRoleService _userRoleService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly LoginCommandValidator _validator;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUserRoleService userRoleService,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        IClock clock,
        LoginCommandValidator validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _userRoleService = userRoleService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(LoginCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<AuthTokenDto>(AuthenticationErrors.InvalidLoginRequest(validationErrors));
        }

        var user = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
        if (user == null)
        {
            return Result.Failure<AuthTokenDto>(AuthenticationErrors.InvalidCredentials);
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokenDto>(AuthenticationErrors.InvalidCredentials);
        }

        if (!user.IsAuthenticated)
        {
            user.SetAuthenticated(true, _clock.UtcNow);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var roles = await _userRoleService.GetUserRolesAsync(user.Id, cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        var userDto = new AuthUserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Status,
            user.IsAuthenticated);

        var response = new AuthTokenDto(
            token,
            "Bearer",
            _jwtTokenGenerator.GetAccessTokenExpirySeconds(),
            userDto);

        return Result.Success(response);
    }
}
