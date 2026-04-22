namespace Pkm.Application.Features.Documents.Models;

public sealed record BlockDto(
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