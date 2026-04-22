namespace Pkm.Application.Features.Documents.Commands.MoveBlock;

public sealed record MoveBlockCommand(
    Guid BlockId,
    long ExpectedRevision,
    string EditorSessionId,
    Guid? NewParentBlockId,
    Guid? PreviousBlockId,
    Guid? NextBlockId);