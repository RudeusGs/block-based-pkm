namespace Pkm.Api.Contracts.Responses.Auth;

public sealed record AuthUserResponse(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Status,
    bool IsAuthenticated,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthUserResponse User);
