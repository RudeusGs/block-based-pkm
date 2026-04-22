namespace Pkm.Application.Features.Documents.Commands.CreateBlock;

public sealed record CreateBlockCommand(
    Guid PageId,
    string Type,
    string? TextContent,
    string? PropsJson,
    Guid? ParentBlockId,
    Guid? PreviousBlockId,
    Guid? NextBlockId,
    int SchemaVersion = 1);