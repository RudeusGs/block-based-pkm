namespace Pkm.Api.Contracts.Requests.Blocks;

public sealed record CreateBlockRequest(
    string Type,
    string? TextContent,
    string? PropsJson,
    Guid? ParentBlockId = null,
    Guid? PreviousBlockId = null,
    Guid? NextBlockId = null,
    int SchemaVersion = 1);