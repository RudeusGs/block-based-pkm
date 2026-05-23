using Pkm.Domain.Users;

namespace Pkm.Application.Features.Account.Models;

public static class UserProfileMappings
{
    public static UserProfileDto ToProfileDto(this User user)
        => new(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Status.ToString(),
            user.CreatedDate,
            user.UpdatedDate);
}
