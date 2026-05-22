namespace Pkm.Application.Features.Files.Commands.UploadPageCoverImage;

public sealed record UploadPageCoverImageCommand(
    Guid PageId,
    long? ExpectedRevision,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
