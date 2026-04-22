namespace Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;

public sealed record UpdatePageMetadataCommand(
    Guid PageId,
    string Title,
    string? Icon,
    string? CoverImage);