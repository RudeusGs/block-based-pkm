namespace Pkm.Application.Features.Documents.Commands.AcquireBlockLease;

public sealed record AcquireBlockLeaseCommand(
    Guid BlockId,
    string EditorSessionId,
    string? HolderDisplayName = null);