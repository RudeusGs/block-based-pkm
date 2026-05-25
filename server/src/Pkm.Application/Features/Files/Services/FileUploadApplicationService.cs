using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Storage;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Files.Models;
using Pkm.Domain.Files;

namespace Pkm.Application.Features.Files.Services;

public sealed class FileUploadApplicationService : IFileUploadApplicationService
{
    private const long MaxImageSizeBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    private readonly IFileStorageService _fileStorageService;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IClock _clock;

    public FileUploadApplicationService(
        IFileStorageService fileStorageService,
        IStoredFileRepository storedFileRepository,
        IClock clock)
    {
        _fileStorageService = fileStorageService;
        _storedFileRepository = storedFileRepository;
        _clock = clock;
    }

    public async Task<Result<StoredFileDto>> UploadImageAsync(
        UploadImageInput input,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateImage(input);
        if (validationError is not null)
            return Result.Failure<StoredFileDto>(validationError);

        try
        {
            var uploadResult = await _fileStorageService.UploadAsync(
                new FileStorageUploadRequest(
                    input.Content,
                    input.FileName,
                    input.ContentType,
                    input.SizeBytes,
                    input.Purpose),
                cancellationToken);

            var storedFile = StoredFile.Create(
                Guid.NewGuid(),
                input.UploadedByUserId,
                uploadResult.Provider,
                uploadResult.StorageFileId,
                uploadResult.FileName,
                SafeOriginalFileName(input.FileName),
                input.ContentType,
                input.SizeBytes,
                uploadResult.PublicUrl,
                uploadResult.WebViewLink,
                input.Purpose,
                _clock.UtcNow);

            _storedFileRepository.Add(storedFile);

            return Result.Success(storedFile.ToDto());
        }
        catch (FileStorageException ex)
        {
            return Result.Failure<StoredFileDto>(FileErrors.UploadFailed(ex.Message));
        }
    }

    private static Error? ValidateImage(UploadImageInput input)
    {
        if (input.UploadedByUserId == Guid.Empty)
            return FileErrors.MissingUserContext;

        if (input.Content is null)
            return FileErrors.MissingFile;

        if (input.SizeBytes <= 0)
            return FileErrors.EmptyFile;

        if (input.SizeBytes > MaxImageSizeBytes)
            return FileErrors.ImageTooLarge;

        if (!AllowedImageContentTypes.Contains(input.ContentType))
            return FileErrors.UnsupportedImageType;

        return null;
    }

    private static string SafeOriginalFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName ?? string.Empty).Trim();

        return string.IsNullOrWhiteSpace(safe)
            ? "uploaded-image"
            : safe;
    }
}
