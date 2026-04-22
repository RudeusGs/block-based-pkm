namespace Pkm.Api.Contracts.Requests.Auth;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string? AvatarUrl);