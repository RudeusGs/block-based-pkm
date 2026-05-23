namespace Pkm.Api.Contracts.Requests.Account;

public sealed record UpdateMyProfileRequest(
    string FullName,
    string? AvatarUrl);
