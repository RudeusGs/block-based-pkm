namespace Pkm.Application.Features.Documents.Models;

public sealed record BlockLeaseDto(
    Guid BlockId,
    Guid PageId,
    bool Granted,
    string Status,
    Guid? HolderUserId,
    string? HolderDisplayName,
    DateTimeOffset? ExpiresAtUtc,
    bool IsHeldByCurrentUser);