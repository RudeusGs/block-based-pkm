using AppAuth = Pkm.Application.Features.Authentication.Models;

namespace Pkm.Api.Contracts.Responses.Auth;

public static class AuthResponseMappings
{
    public static AuthUserResponse ToResponse(this AppAuth.AuthUserDto dto)
        => new(
            dto.Id,
            dto.UserName,
            dto.Email,
            dto.FullName,
            dto.AvatarUrl,
            dto.Status,
            dto.IsAuthenticated,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static AuthTokenResponse ToResponse(this AppAuth.AuthTokenDto dto)
        => new(
            dto.AccessToken,
            dto.RefreshToken,
            dto.TokenType,
            dto.ExpiresIn,
            dto.RefreshTokenExpiresAtUtc,
            dto.User.ToResponse());
}
