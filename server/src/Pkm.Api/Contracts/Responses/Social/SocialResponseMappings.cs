using Pkm.Application.Features.Social.Models;

namespace Pkm.Api.Contracts.Responses.Social;

public static class SocialResponseMappings
{
    public static UserSummaryResponse ToResponse(this UserSummaryDto dto)
        => new(
            dto.Id,
            dto.UserName,
            dto.FullName,
            dto.AvatarUrl);

    public static UserSearchResultResponse ToResponse(this UserSearchResultDto dto)
        => new(
            dto.Id,
            dto.UserName,
            dto.FullName,
            dto.AvatarUrl,
            dto.IsCurrentUser,
            dto.FriendshipStatus);

    public static FriendRequestResponse ToResponse(this FriendRequestDto dto)
        => new(
            dto.Id,
            dto.RequesterId,
            dto.AddresseeId,
            dto.Status.ToString(),
            dto.OtherUser.ToResponse(),
            dto.CreatedDate,
            dto.RespondedAtUtc);

    public static FriendResponse ToResponse(this FriendDto dto)
        => new(
            dto.UserId,
            dto.UserName,
            dto.FullName,
            dto.AvatarUrl,
            dto.FriendsSinceUtc);

    public static ProfileWorkspaceResponse ToResponse(this ProfileWorkspaceDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Visibility.ToString(),
            dto.CreatedDate,
            dto.UpdatedDate);

    public static UserProfilePageResponse ToResponse(this UserProfilePageDto dto)
        => new(
            dto.UserId,
            dto.UserName,
            dto.FullName,
            dto.AvatarUrl,
            dto.Bio,
            dto.CoverImageUrl,
            dto.FriendshipStatus,
            dto.FriendCount,
            dto.Workspaces.Select(x => x.ToResponse()).ToArray(),
            dto.CreatedDate,
            dto.UpdatedDate);
}
