namespace Pkm.Api.Contracts.Responses.Blocks;

public sealed record BlockResponse(
    Guid Id,
    Guid PageId,
    Guid? ParentBlockId,
    string Type,
    string? TextContent,
    string? PropsJson,
    int SchemaVersion,
    string OrderKey,
    Guid CreatedBy,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record BlockLeaseResponse(
    Guid BlockId,
    Guid PageId,
    bool Granted,
    string Status,
    Guid? HolderUserId,
    string? HolderDisplayName,
    DateTimeOffset? ExpiresAtUtc,
    bool IsHeldByCurrentUser);

public sealed record BlockMutationResponse(
    Guid PageId,
    Guid? BlockId,
    long AppliedRevision,
    BlockResponse? Block);

public sealed record PageDocumentResponse(
    Guid PageId,
    long CurrentRevision,
    IReadOnlyList<BlockResponse> Blocks);