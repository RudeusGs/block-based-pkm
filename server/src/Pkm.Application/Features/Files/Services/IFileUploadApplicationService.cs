using Pkm.Application.Common.Results;
using Pkm.Application.Features.Files.Models;

namespace Pkm.Application.Features.Files.Services;

public sealed record UploadImageInput(
    Guid UploadedByUserId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string? Purpose);

public interface IFileUploadApplicationService
{
    Task<Result<StoredFileDto>> UploadImageAsync(
        UploadImageInput input,
        CancellationToken cancellationToken = default);
}
