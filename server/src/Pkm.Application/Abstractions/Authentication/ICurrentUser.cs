namespace Pkm.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? UserName { get; }

    string? Email { get; }

    bool TryGetUserId(out Guid userId);
}