using Microsoft.AspNetCore.Http;

namespace Pkm.Api.Contracts.Requests.Social;

public sealed record SendFriendRequestRequest(
    Guid AddresseeUserId);

public sealed record UpdateMyProfilePageRequest(
    string? Bio,
    string? CoverImageUrl);

public sealed class UploadProfileCoverImageFormRequest
{
    public IFormFile? File { get; init; }
}
