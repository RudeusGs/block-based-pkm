namespace Pkm.Api.Contracts.Requests.Blocks;

public sealed record RenewBlockLeaseRequest(
    string EditorSessionId);