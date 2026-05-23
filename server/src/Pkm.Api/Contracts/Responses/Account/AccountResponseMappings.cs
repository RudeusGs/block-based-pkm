using Pkm.Application.Features.Account.Models;

namespace Pkm.Api.Contracts.Responses.Account;

public static class AccountResponseMappings
{
    public static UserProfileResponse ToResponse(this UserProfileDto dto)
        => new(
            dto.Id,
            dto.UserName,
            dto.Email,
            dto.FullName,
            dto.AvatarUrl,
            dto.Status,
            dto.CreatedDate,
            dto.UpdatedDate);
}
