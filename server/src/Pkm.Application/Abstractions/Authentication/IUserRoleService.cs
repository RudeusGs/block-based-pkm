namespace Pkm.Application.Abstractions.Authentication;

public interface IUserRoleService
{
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignDefaultRoleAsync(Guid userId, CancellationToken cancellationToken = default);
}
