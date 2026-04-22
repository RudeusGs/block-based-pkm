namespace Pkm.Api.Contracts.Responses.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Status,
    bool IsAuthenticated);

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthUserResponse User);