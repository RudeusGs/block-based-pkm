namespace Pkm.Api.Contracts.Requests.Blocks;

public sealed record AcquireBlockLeaseRequest(
    string EditorSessionId,
    string? HolderDisplayName = null);