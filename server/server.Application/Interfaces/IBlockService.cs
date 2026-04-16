using server.Service.Models;
using server.Service.Models.Block;

namespace server.Application.Interfaces
{
    public interface IBlockService
    {
        Task<ApiResult> GetByIdAsync(int blockId, CancellationToken ct = default);
        Task<ApiResult> GetByPageAsync(int pageId, CancellationToken ct = default);

        Task<ApiResult> CreateBlockAsync(AddBlockModel model, CancellationToken ct = default);
        Task<ApiResult> UpdateBlockAsync(int blockId, UpdateBlockModel model, CancellationToken ct = default);
        Task<ApiResult> MoveBlockAsync(int blockId, MoveBlockModel model, CancellationToken ct = default);
        Task<ApiResult> DeleteBlockAsync(int blockId, CancellationToken ct = default);
    }
}
