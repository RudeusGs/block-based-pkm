using Pkm.Domain.Users;

namespace Pkm.Application.Abstractions.Authentication;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListActiveByUserForUpdateAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);

    void Update(RefreshToken refreshToken);
}
