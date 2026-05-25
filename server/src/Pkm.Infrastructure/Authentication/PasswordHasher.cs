using Pkm.Application.Common.Abstractions.Authentication;


namespace Pkm.Infrastructure.Authentication;

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
