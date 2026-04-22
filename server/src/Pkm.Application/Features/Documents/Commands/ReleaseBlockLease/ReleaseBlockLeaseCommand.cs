namespace Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;

public sealed record ReleaseBlockLeaseCommand(
    Guid BlockId,
    string EditorSessionId);