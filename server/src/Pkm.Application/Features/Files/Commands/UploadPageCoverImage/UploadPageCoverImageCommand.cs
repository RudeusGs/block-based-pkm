using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Files.Commands.UploadPageCoverImage;

public sealed record UploadPageCoverImageCommand(
    Guid PageId,
    long? ExpectedRevision,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : ICommand<PageDto>;
