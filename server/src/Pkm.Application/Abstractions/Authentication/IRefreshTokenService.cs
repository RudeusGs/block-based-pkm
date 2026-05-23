namespace Pkm.Application.Abstractions.Authentication;

public interface IRefreshTokenService
{
    RefreshTokenCandidate Create(DateTimeOffset now);

    string Hash(string rawRefreshToken);
}

public sealed record RefreshTokenCandidate(
    string RawToken,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc);
