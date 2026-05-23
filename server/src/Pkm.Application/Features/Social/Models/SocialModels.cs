using Pkm.Domain.Social;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Social.Models;

public sealed record UserSummaryDto(
    Guid Id,
    string UserName,
    string FullName,
    string? AvatarUrl);

public sealed record UserSearchResultDto(
    Guid Id,
    string UserName,
    string FullName,
    string? AvatarUrl,
    bool IsCurrentUser,
    string FriendshipStatus);

public sealed record FriendRequestDto(
    Guid Id,
    Guid RequesterId,
    Guid AddresseeId,
    FriendRequestStatus Status,
    UserSummaryDto OtherUser,
    DateTimeOffset CreatedDate,
    DateTimeOffset? RespondedAtUtc);

public sealed record FriendDto(
    Guid UserId,
    string UserName,
    string FullName,
    string? AvatarUrl,
    DateTimeOffset FriendsSinceUtc);

public sealed record ProfileWorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    WorkspaceVisibility Visibility,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record UserProfilePageDto(
    Guid UserId,
    string UserName,
    string FullName,
    string? AvatarUrl,
    string? Bio,
    string? CoverImageUrl,
    string FriendshipStatus,
    int FriendCount,
    IReadOnlyList<ProfileWorkspaceDto> Workspaces,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);
