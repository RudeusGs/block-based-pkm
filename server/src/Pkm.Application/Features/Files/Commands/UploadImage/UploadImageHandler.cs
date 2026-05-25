using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Files.Models;
using Pkm.Application.Features.Files.Services;

namespace Pkm.Application.Features.Files.Commands.UploadImage;

public sealed class UploadImageHandler : ICommandHandler<UploadImageCommand, StoredFileDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadImageHandler(
        ICurrentUser currentUser,
        IFileUploadApplicationService fileUploadApplicationService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _fileUploadApplicationService = fileUploadApplicationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StoredFileDto>> HandleAsync(
        UploadImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<StoredFileDto>(FileErrors.MissingUserContext);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Content,
                request.Purpose),
            cancellationToken);

        if (uploadResult.IsFailure)
            return uploadResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return uploadResult;
    }
}
