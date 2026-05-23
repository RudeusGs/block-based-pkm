namespace Pkm.Application.Features.Account.Models;

public sealed record UserProfileDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Status,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);
