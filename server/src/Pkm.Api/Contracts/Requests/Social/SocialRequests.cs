namespace Pkm.Api.Contracts.Requests.Social;

public sealed record SendFriendRequestRequest(
    Guid AddresseeUserId);

public sealed record UpdateMyProfilePageRequest(
    string? Bio,
    string? CoverImageUrl);
