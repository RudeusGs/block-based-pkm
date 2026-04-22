using Pkm.Application.Abstractions.Authentication;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Authentication;

internal sealed class UserRoleService : IUserRoleService
{
    // Transitional implementation: RBAC persistence is not introduced in this phase.
    // Keep one canonical default role so token generation and role queries stay consistent.
    private static readonly IReadOnlyList<string> DefaultRoles = [SystemRoles.RegularUser];

    public Task<IEnumerable<string>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<string>>(DefaultRoles);

    public Task AssignDefaultRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
