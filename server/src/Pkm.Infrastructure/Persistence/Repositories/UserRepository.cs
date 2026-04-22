using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Repositories;

/// <summary>
/// Triển khai IUserRepository dùng EF Core DataContext.
/// Chỉ chứa query/command thuần, không chứa business logic.
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly DataContext _dbContext;

    public UserRepository(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> IsUserNameUniqueAsync(string userName, CancellationToken cancellationToken = default)
        => !await _dbContext.Users
            .AnyAsync(u => u.UserName == userName, cancellationToken);

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
        => !await _dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user)
        => _dbContext.Users.Add(user);

    public void Update(User user)
        => _dbContext.Users.Update(user);
}
