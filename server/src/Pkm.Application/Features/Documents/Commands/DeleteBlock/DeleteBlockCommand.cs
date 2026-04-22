namespace Pkm.Application.Features.Documents.Commands.DeleteBlock;

public sealed record DeleteBlockCommand(
    Guid BlockId,
    long ExpectedRevision,
    string EditorSessionId,
    string? Note = null);