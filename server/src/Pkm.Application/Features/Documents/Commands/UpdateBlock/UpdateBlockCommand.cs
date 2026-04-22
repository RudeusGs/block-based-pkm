namespace Pkm.Application.Features.Documents.Commands.UpdateBlock;

public sealed record UpdateBlockCommand(
    Guid BlockId,
    long ExpectedRevision,
    string EditorSessionId,
    string? TextContent,
    string? PropsJson,
    string? Type = null);