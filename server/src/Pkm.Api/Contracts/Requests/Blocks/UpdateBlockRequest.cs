namespace Pkm.Api.Contracts.Requests.Blocks;

public sealed record UpdateBlockRequest(
    long ExpectedRevision,
    string EditorSessionId,
    string? TextContent,
    string? PropsJson,
    string? Type = null);