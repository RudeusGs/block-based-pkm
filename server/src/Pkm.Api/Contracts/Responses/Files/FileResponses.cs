using Pkm.Application.Features.Files.Models;

namespace Pkm.Api.Contracts.Responses.Files;

public sealed record StoredFileResponse(
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

public static class FileResponseMappings
{
    public static StoredFileResponse ToResponse(this StoredFileDto dto)
        => new(
            dto.Id,
            dto.UploadedByUserId,
            dto.StorageProvider,
            dto.StorageFileId,
            dto.FileName,
            dto.OriginalFileName,
            dto.ContentType,
            dto.SizeBytes,
            dto.PublicUrl,
            dto.WebViewLink,
            dto.Purpose,
            dto.CreatedDate);
}
