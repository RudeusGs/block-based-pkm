namespace Pkm.Api.Contracts.Responses.Account;

public sealed record UserProfileResponse(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Status,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);
