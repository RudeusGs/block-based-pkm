namespace Pkm.Api.Contracts.Responses.Social;

public sealed record UserSummaryResponse(
    Guid Id,
    string UserName,
    string FullName,
    string? AvatarUrl);

public sealed record UserSearchResultResponse(
    Guid Id,
    string UserName,
    string FullName,
    string? AvatarUrl,
    bool IsCurrentUser,
    string FriendshipStatus);

public sealed record FriendRequestResponse(
    Guid Id,
    Guid RequesterId,
    Guid AddresseeId,
    string Status,
    UserSummaryResponse OtherUser,
    DateTimeOffset CreatedDate,
    DateTimeOffset? RespondedAtUtc);

public sealed record FriendResponse(
    Guid UserId,
    string UserName,
    string FullName,
    string? AvatarUrl,
    DateTimeOffset FriendsSinceUtc);

public sealed record UserSearchResultPagedResultResponse(
    IReadOnlyList<UserSearchResultResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record FriendRequestPagedResultResponse(
    IReadOnlyList<FriendRequestResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record FriendPagedResultResponse(
    IReadOnlyList<FriendResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);


public sealed record ProfileWorkspaceResponse(
    Guid Id,
    string Name,
    string? Description,
    string Visibility,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record UserProfilePageResponse(
    Guid UserId,
    string UserName,
    string FullName,
    string? AvatarUrl,
    string? Bio,
    string? CoverImageUrl,
    string FriendshipStatus,
    int FriendCount,
    IReadOnlyList<ProfileWorkspaceResponse> Workspaces,
    int WorkspacePageNumber,
    int WorkspacePageSize,
    int WorkspaceTotalCount,
    int WorkspaceTotalPages,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);
