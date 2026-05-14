using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Authentication;

namespace Pkm.Infrastructure.Authentication;

internal sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenBytesLength = 64;

    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public RefreshTokenCandidate Create(DateTimeOffset now)
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytesLength);
        var rawToken = Convert.ToBase64String(bytes);

        return new RefreshTokenCandidate(
            rawToken,
            Hash(rawToken),
            now.AddDays(_jwtSettings.RefreshTokenExpiryDays));
    }

    public string Hash(string rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(rawRefreshToken);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }
}