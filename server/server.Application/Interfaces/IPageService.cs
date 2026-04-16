using server.Service.Models;
using server.Service.Models.Page;

namespace server.Service.Interfaces
{
    public interface IPageService
    {
        Task<ApiResult> CreatePageAsync(AddPageModel model, CancellationToken ct = default);
        Task<ApiResult> UpdatePageAsync(int pageId, UpdatePageModel model, CancellationToken ct = default);
        Task<ApiResult> DeletePageAsync(int pageId, CancellationToken ct = default);
        Task<ApiResult> GetPageByIdAsync(int pageId, CancellationToken ct = default);
        Task<ApiResult> GetPagesByWorkspaceAsync(int workspaceId, PagingRequest? paging = null, CancellationToken ct = default);
        Task<ApiResult> GetSubPagesAsync(int parentPageId, CancellationToken ct = default);
        Task<ApiResult> SearchPagesAsync(int workspaceId, string keyword, PagingRequest? paging = null, CancellationToken ct = default);
    }
}