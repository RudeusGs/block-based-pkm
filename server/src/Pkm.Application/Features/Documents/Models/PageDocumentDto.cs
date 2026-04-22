namespace Pkm.Application.Features.Documents.Models;

public sealed record PageDocumentDto(
    Guid PageId,
    long CurrentRevision,
    IReadOnlyList<BlockDto> Blocks);