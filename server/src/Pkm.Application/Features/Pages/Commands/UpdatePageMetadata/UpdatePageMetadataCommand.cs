namespace Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;

public sealed record UpdatePageMetadataCommand(
    Guid PageId,
    long ExpectedRevision,
    string Title,
    string? Icon,
    string? CoverImage);
