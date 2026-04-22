namespace Pkm.Application.Features.Documents.Commands.RenewBlockLease;

public sealed record RenewBlockLeaseCommand(
    Guid BlockId,
    string EditorSessionId);