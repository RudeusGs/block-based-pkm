using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly DataContext _dbContext;

    public UserRepository(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeUserName(userName);

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.NormalizedUserName == normalized,
                cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalized,
                cancellationToken);
    }

    public async Task<User?> GetByEmailForUpdateAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);

        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalized,
                cancellationToken);
    }

    public async Task<User?> GetByLoginIdentifierAsync(
        string loginIdentifier,
        CancellationToken cancellationToken = default)
    {
        var trimmed = loginIdentifier.Trim();

        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            var normalizedEmail = User.NormalizeEmail(trimmed);

            return await _dbContext.Users
                .FirstOrDefaultAsync(
                    u => u.NormalizedEmail == normalizedEmail,
                    cancellationToken);
        }

        var normalizedUserName = User.NormalizeUserName(trimmed);

        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.NormalizedUserName == normalizedUserName,
                cancellationToken);
    }

    public async Task<bool> IsUserNameUniqueAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeUserName(userName);

        return !await _dbContext.Users
            .AnyAsync(
                u => u.NormalizedUserName == normalized,
                cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);

        return !await _dbContext.Users
            .AnyAsync(
                u => u.NormalizedEmail == normalized,
                cancellationToken);
    }

    public void Add(User user)
        => _dbContext.Users.Add(user);

    public void Update(User user)
        => _dbContext.Users.Update(user);
}
