using server.Service.Models;
using server.Service.Models.Workspace;

public interface IWorkspaceService
{
    Task<ApiResult> CreateWorkspaceAsync(AddWorkspaceModel model, CancellationToken ct = default);
    Task<ApiResult> UpdateWorkspaceAsync(UpdateWorkspaceModel model, CancellationToken ct = default);
    Task<ApiResult> DeleteWorkspaceAsync(int workspaceId, CancellationToken ct = default);
    Task<ApiResult> GetAllByUserIdAsync(int userId, PagingRequest paging, CancellationToken ct = default);
    Task<ApiResult> GetWorkspaceByIdAsync(int id, CancellationToken ct = default);
}