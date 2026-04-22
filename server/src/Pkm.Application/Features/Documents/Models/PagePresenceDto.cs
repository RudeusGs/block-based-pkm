namespace Pkm.Application.Features.Documents.Models;

public sealed record PagePresenceUserDto(
    Guid UserId,
    string? UserName,
    int ConnectionCount,
    DateTimeOffset LastSeenUtc);

public sealed record PagePresenceDto(
    Guid WorkspaceId,
    Guid PageId,
    IReadOnlyList<PagePresenceUserDto> ActiveUsers);