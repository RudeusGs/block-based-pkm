using Pkm.Domain.Files;

namespace Pkm.Application.Features.Files.Models;

public sealed record StoredFileDto(
    Guid Id,
    Guid UploadedByUserId,
    string StorageProvider,
    string StorageFileId,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string PublicUrl,
    string? WebViewLink,
    string? Purpose,
    DateTimeOffset CreatedDate);

public static class StoredFileMappings
{
    public static StoredFileDto ToDto(this StoredFile storedFile)
        => new(
            storedFile.Id,
            storedFile.UploadedByUserId,
            storedFile.StorageProvider,
            storedFile.StorageFileId,
            storedFile.FileName,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.PublicUrl,
            storedFile.WebViewLink,
            storedFile.Purpose,
            storedFile.CreatedDate);
}
