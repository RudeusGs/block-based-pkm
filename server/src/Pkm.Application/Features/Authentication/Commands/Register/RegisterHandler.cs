using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Authentication;
using Pkm.Application.Features.Authentication.Models;
using Pkm.Domain.Common;
using Pkm.Domain.Users;

namespace Pkm.Application.Features.Authentication.Commands.Register;

public sealed class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRoleService _userRoleService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RegisterCommandValidator _validator;

    public RegisterHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUserRoleService userRoleService,
        IUnitOfWork unitOfWork,
        IClock clock,
        RegisterCommandValidator validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _userRoleService = userRoleService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<AuthUserDto>> HandleAsync(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<AuthUserDto>(AuthenticationErrors.InvalidRegisterRequest(validationErrors));
        }

        try
        {
            if (!await _userRepository.IsUserNameUniqueAsync(request.UserName, cancellationToken))
                return Result.Failure<AuthUserDto>(AuthenticationErrors.DuplicateUserName);

            if (!await _userRepository.IsEmailUniqueAsync(request.Email, cancellationToken))
                return Result.Failure<AuthUserDto>(AuthenticationErrors.DuplicateEmail);

            var user = User.Create(
                Guid.NewGuid(),
                request.UserName,
                request.Email,
                request.FullName,
                request.AvatarUrl,
                request.Password,
                _passwordHasher,
                _clock.UtcNow);

            _userRepository.Add(user);

            await _userRoleService.AssignDefaultRoleAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new AuthUserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.AvatarUrl,
                user.Status,
                user.IsAuthenticated));
        }
        catch (DomainException ex)
        {
            return Result.Failure<AuthUserDto>(new Error(
                AuthenticationErrors.InvalidRegisterData.Code,
                ex.Message,
                AuthenticationErrors.InvalidRegisterData.Status));
        }
    }
}
