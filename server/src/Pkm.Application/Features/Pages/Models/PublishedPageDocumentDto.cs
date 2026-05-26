using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Pages.Models;

public sealed record PublishedPageDocumentDto(
    Guid PageId,
    string Title,
    string? Icon,
    string? CoverImage,
    long CurrentRevision,
    DateTimeOffset PublishedAt,
    IReadOnlyList<BlockDto> Blocks,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
