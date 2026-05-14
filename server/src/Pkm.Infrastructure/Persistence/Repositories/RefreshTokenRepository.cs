using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DataContext _context;

    public RefreshTokenRepository(DataContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserForUpdateAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(x =>
                x.UserId == userId &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
        => _context.RefreshTokens.Add(refreshToken);

    public void Update(RefreshToken refreshToken)
        => _context.RefreshTokens.Update(refreshToken);
}