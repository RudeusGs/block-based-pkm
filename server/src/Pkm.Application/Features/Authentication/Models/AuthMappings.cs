using Pkm.Domain.Users;

namespace Pkm.Application.Features.Authentication.Models;

public static class AuthMappings
{
    public static AuthUserDto ToAuthUserDto(
        this User user,
        bool isAuthenticated)
        => new(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Status.ToString(),
            isAuthenticated,
            user.CreatedDate,
            user.UpdatedDate);
}