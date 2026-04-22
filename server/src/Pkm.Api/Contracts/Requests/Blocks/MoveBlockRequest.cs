namespace Pkm.Api.Contracts.Requests.Blocks;

public sealed record MoveBlockRequest(
    long ExpectedRevision,
    string EditorSessionId,
    Guid? NewParentBlockId,
    Guid? PreviousBlockId,
    Guid? NextBlockId);