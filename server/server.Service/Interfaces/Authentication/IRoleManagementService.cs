namespace server.Service.Interfaces.Authentication
{
    public interface IRoleManagementService
    {
        Task EnsureRegularRoleExistsAsync(CancellationToken ct = default);

        Task EnsureRolesExistAsync(CancellationToken ct = default);

        Task<(bool succeeded, List<string> errors)> AddToRegularRoleAsync(int userId, CancellationToken ct = default);
    }
}
