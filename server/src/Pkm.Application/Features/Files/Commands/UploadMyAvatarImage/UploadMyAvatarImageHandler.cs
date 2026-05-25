using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;
using Pkm.Application.Features.Authentication;
using Pkm.Application.Features.Files.Services;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Files.Commands.UploadMyAvatarImage;

public sealed class UploadMyAvatarImageHandler : ICommandHandler<UploadMyAvatarImageCommand, UserProfileDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UploadMyAvatarImageHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFileUploadApplicationService fileUploadApplicationService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _fileUploadApplicationService = fileUploadApplicationService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<UserProfileDto>> HandleAsync(
        UploadMyAvatarImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfileDto>(AuthenticationErrors.MissingUserContext);

        var user = await _userRepository.GetByIdForUpdateAsync(
            currentUserId,
            cancellationToken);

        if (user is null)
            return Result.Failure<UserProfileDto>(AuthenticationErrors.UserNotFound);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Content,
                "avatar"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<UserProfileDto>(uploadResult.Error);

        try
        {
            user.UpdateProfile(
                user.FullName,
                uploadResult.Value.PublicUrl,
                _clock.UtcNow);

            _userRepository.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.ToProfileDto());
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfileDto>(new Error(
                "File.UpdateAvatarFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}
