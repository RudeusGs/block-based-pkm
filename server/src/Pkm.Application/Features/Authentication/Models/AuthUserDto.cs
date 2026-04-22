namespace Pkm.Application.Features.Authentication.Models;

public sealed record AuthUserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string Status,
    bool IsAuthenticated);
