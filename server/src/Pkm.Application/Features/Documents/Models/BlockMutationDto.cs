namespace Pkm.Application.Features.Documents.Models;

public sealed record BlockMutationDto(
    Guid PageId,
    Guid? BlockId,
    long AppliedRevision,
    BlockDto? Block);