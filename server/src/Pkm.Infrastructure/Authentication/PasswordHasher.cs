using Pkm.Domain.Users;


namespace Pkm.Infrastructure.Authentication;

/// <summary>
/// Adapter from domain password hashing abstraction to ASP.NET Core PasswordHasher.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<string> _innerHasher = new();

    public string HashPassword(string rawPassword)
    {
        return _innerHasher.HashPassword(string.Empty, rawPassword);
    }

    public bool VerifyPassword(string rawPassword, string hashedPassword)
    {
        var result = _innerHasher.VerifyHashedPassword(string.Empty, hashedPassword, rawPassword);
        return result != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
    }
}
