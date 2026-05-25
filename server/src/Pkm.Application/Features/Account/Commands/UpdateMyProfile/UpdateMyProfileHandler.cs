using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;
using Pkm.Application.Features.Authentication;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Account.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileHandler : ICommandHandler<UpdateMyProfileCommand, UserProfileDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly UpdateMyProfileCommandValidator _validator;

    public UpdateMyProfileHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        UpdateMyProfileCommandValidator validator)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<UserProfileDto>> HandleAsync(
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<UserProfileDto>(
                AccountErrors.InvalidUpdateProfileRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<UserProfileDto>(
                AuthenticationErrors.MissingUserContext);
        }

        var user = await _userRepository.GetByIdForUpdateAsync(
            currentUserId,
            cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserProfileDto>(
                AuthenticationErrors.UserNotFound);
        }

        try
        {
            user.UpdateProfile(
                request.FullName,
                request.AvatarUrl,
                _clock.UtcNow);

            _userRepository.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.ToProfileDto());
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfileDto>(new Error(
                AccountErrors.InvalidProfileData.Code,
                ex.Message,
                AccountErrors.InvalidProfileData.Status));
        }
    }
}
