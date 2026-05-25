using Microsoft.AspNetCore.Http;

namespace Pkm.Api.Contracts.Requests.Files;

public sealed class UploadImageFormRequest
{
    public IFormFile? File { get; init; }
    public string? Purpose { get; init; }
}

public sealed class UploadAvatarImageFormRequest
{
    public IFormFile? File { get; init; }
}

public sealed class UploadPageCoverImageFormRequest
{
    public IFormFile? File { get; init; }
    public long? ExpectedRevision { get; init; }
}
