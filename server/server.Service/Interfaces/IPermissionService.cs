namespace server.Service.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> HasWorkspaceAccessAsync(int workspaceId, int userId, CancellationToken ct);
    }
}
