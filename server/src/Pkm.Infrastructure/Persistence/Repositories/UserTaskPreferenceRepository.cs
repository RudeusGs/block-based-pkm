using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Recommendations;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class UserTaskPreferenceRepository : IUserTaskPreferenceRepository
{
    private readonly DataContext _context;

    public UserTaskPreferenceRepository(DataContext context)
    {
        _context = context;
    }

    public Task<UserTaskPreference?> GetByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserTaskPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkspaceId == workspaceId,
                cancellationToken);
    }

    public Task<UserTaskPreference?> GetByUserAndWorkspaceForUpdateAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return _context.UserTaskPreferences
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.WorkspaceId == workspaceId,
                cancellationToken);
    }

    public void Add(UserTaskPreference preference)
    {
        _context.UserTaskPreferences.Add(preference);
    }

    public void Update(UserTaskPreference preference)
    {
        _context.UserTaskPreferences.Update(preference);
    }
}