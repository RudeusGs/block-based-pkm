using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pkm.Application.Abstractions.Authentication;

namespace Pkm.Infrastructure.Authentication;

internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw =
                Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                Principal?.FindFirstValue("sub");

            return Guid.TryParse(raw, out var userId) && userId != Guid.Empty
                ? userId
                : null;
        }
    }

    public string? UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name) ??
        Principal?.Identity?.Name;

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email);

    public bool TryGetUserId(out Guid userId)
    {
        userId = UserId ?? Guid.Empty;
        return userId != Guid.Empty;
    }
}