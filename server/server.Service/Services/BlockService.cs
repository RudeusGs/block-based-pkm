using Microsoft.EntityFrameworkCore;
using server.Application.Helpers.Blocks;
using server.Application.Interfaces;
using server.Domain.Base;
using server.Domain.Entities;
using server.Domain.Realtime;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.Block;
using server.Service.Services;

namespace server.Application.Services
{
    public class BlockService : BaseService, IBlockService
    {
        private readonly IPermissionService _permissionService;
        private readonly IPresenceService _presenceService;
        private readonly IRealtimeNotifier _notifier;

        private const string DefaultOrderKey = "1000";

        public BlockService(
            DataContext dataContext,
            IUserService userService,
            IPermissionService permissionService,
            IPresenceService presenceService,
            IRealtimeNotifier notifier)
            : base(dataContext, userService)
        {
            _permissionService = permissionService;
            _presenceService = presenceService;
            _notifier = notifier;
        }

        public async Task<ApiResult> CreateBlockAsync(AddBlockModel model, CancellationToken ct = default)
        {
            try
            {
                if (model == null || model.PageId <= 0)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                if (!BlockMapper.TryParseBlockType(model.Type, out var blockType))
                    return ApiResult.Fail("Loại block không hợp lệ", "INVALID_BLOCK_TYPE");

                var pageCtx = await BlockAccessHelper.GetPageAccessContextAsync(
                    _dataContext, _userService, _permissionService, model.PageId, ct);

                await BlockAccessHelper.EnsureParentBlockBelongsToPageAsync(
                    _dataContext, model.PageId, model.ParentBlockId, ct);

                await BlockRealtimeHelper.TouchPagePresenceAsync(
                    _presenceService, pageCtx.Page.Id, pageCtx.UserId, UserName, ct);

                var block = new Block(
                    pageId: model.PageId,
                    type: blockType,
                    orderKey: string.IsNullOrWhiteSpace(model.OrderKey) ? DefaultOrderKey : model.OrderKey.Trim(),
                    createdBy: pageCtx.UserId,
                    textContent: model.TextContent,
                    propsJson: model.PropsJson,
                    parentBlockId: model.ParentBlockId
                );

                _dataContext.Set<Block>().Add(block);
                await SaveChangesAsync(ct);

                var response = BlockMapper.ToResponse(block);

                await BlockRealtimeHelper.NotifyWorkspaceSafeAsync(
                    _notifier,
                    pageCtx.Page.WorkspaceId,
                    "BlockCreated",
                    new
                    {
                        WorkspaceId = pageCtx.Page.WorkspaceId,
                        PageId = block.PageId,
                        Block = response,
                        ActorUserId = pageCtx.UserId
                    });

                return ApiResult.Success(response, "Tạo block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResult.Fail("Dữ liệu đã bị thay đổi bởi người khác, vui lòng tải lại", "CONCURRENCY_CONFLICT");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi tạo block", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetByIdAsync(int blockId, CancellationToken ct = default)
        {
            try
            {
                if (blockId <= 0)
                    return ApiResult.Fail("BlockId không hợp lệ", "INVALID_INPUT");

                var blockCtx = await BlockAccessHelper.GetBlockAccessContextAsync(
                    _dataContext, _userService, _permissionService, blockId, ct);

                return ApiResult.Success(BlockMapper.ToResponse(blockCtx.Block), "Lấy block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "NOT_FOUND");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi lấy block", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> GetByPageAsync(int pageId, CancellationToken ct = default)
        {
            try
            {
                if (pageId <= 0)
                    return ApiResult.Fail("PageId không hợp lệ", "INVALID_INPUT");

                var pageCtx = await BlockAccessHelper.GetPageAccessContextAsync(
                    _dataContext, _userService, _permissionService, pageId, ct);

                var blocks = await _dataContext.Set<Block>()
                    .AsNoTracking()
                    .Where(b => b.PageId == pageId && !b.IsDeleted)
                    .OrderBy(b => b.ParentBlockId)
                    .ThenBy(b => b.OrderKey)
                    .ThenBy(b => b.CreatedDate)
                    .ToListAsync(ct);

                var response = blocks.Select(BlockMapper.ToResponse).ToList();
                return ApiResult.Success(response, "Lấy danh sách block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "NOT_FOUND");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi lấy block theo page", "SERVER_ERROR", new[] { ex.Message });
            }
        }

        public async Task<ApiResult> UpdateBlockAsync(int blockId, UpdateBlockModel model, CancellationToken ct = default)
        {
            var lockAcquired = false;
            int currentUserId = 0;

            try
            {
                if (blockId <= 0 || model == null)
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                if (model.TextContent == null && model.PropsJson == null)
                    return ApiResult.Fail("Không có dữ liệu để cập nhật", "NO_CHANGES");

                var blockCtx = await BlockAccessHelper.GetBlockAccessContextAsync(
                    _dataContext, _userService, _permissionService, blockId, ct);

                currentUserId = blockCtx.UserId;

                await BlockRealtimeHelper.TouchPagePresenceAsync(
                    _presenceService, blockCtx.Page.Id, currentUserId, UserName, ct);

                await BlockRealtimeHelper.AcquireEditLockOrThrowAsync(
                    _presenceService, blockId, currentUserId, ct);
                lockAcquired = true;

                if (model.TextContent != null)
                    blockCtx.Block.UpdateText(model.TextContent, currentUserId);

                if (model.PropsJson != null)
                    blockCtx.Block.UpdateProps(model.PropsJson, currentUserId);

                await SaveChangesAsync(ct);

                var response = BlockMapper.ToResponse(blockCtx.Block);

                await BlockRealtimeHelper.NotifyWorkspaceSafeAsync(
                    _notifier,
                    blockCtx.Page.WorkspaceId,
                    "BlockUpdated",
                    new
                    {
                        WorkspaceId = blockCtx.Page.WorkspaceId,
                        PageId = blockCtx.Block.PageId,
                        Block = response,
                        ActorUserId = currentUserId
                    });

                return ApiResult.Success(response, "Cập nhật block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResult.Fail("Block đã bị người khác cập nhật, vui lòng tải lại", "CONCURRENCY_CONFLICT");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi cập nhật block", "SERVER_ERROR", new[] { ex.Message });
            }
            finally
            {
                if (lockAcquired && currentUserId > 0)
                {
                    await BlockRealtimeHelper.ReleaseEditLockSafeAsync(
                        _presenceService, blockId, currentUserId);
                }
            }
        }

        public async Task<ApiResult> MoveBlockAsync(int blockId, MoveBlockModel model, CancellationToken ct = default)
        {
            var lockAcquired = false;
            int currentUserId = 0;

            try
            {
                if (blockId <= 0 || model == null || string.IsNullOrWhiteSpace(model.NewOrderKey))
                    return ApiResult.Fail("Dữ liệu không hợp lệ", "INVALID_INPUT");

                var blockCtx = await BlockAccessHelper.GetBlockAccessContextAsync(
                    _dataContext, _userService, _permissionService, blockId, ct);

                currentUserId = blockCtx.UserId;

                await BlockRealtimeHelper.TouchPagePresenceAsync(
                    _presenceService, blockCtx.Page.Id, currentUserId, UserName, ct);

                await BlockRealtimeHelper.AcquireEditLockOrThrowAsync(
                    _presenceService, blockId, currentUserId, ct);
                lockAcquired = true;

                await BlockAccessHelper.EnsureParentBlockBelongsToPageAsync(
                    _dataContext, blockCtx.Block.PageId, model.NewParentBlockId, ct);

                await BlockTreeHelper.EnsureNoCycleAsync(
                    _dataContext, blockId, model.NewParentBlockId, ct);

                blockCtx.Block.Move(model.NewOrderKey.Trim(), model.NewParentBlockId, currentUserId);
                await SaveChangesAsync(ct);

                var response = BlockMapper.ToResponse(blockCtx.Block);

                await BlockRealtimeHelper.NotifyWorkspaceSafeAsync(
                    _notifier,
                    blockCtx.Page.WorkspaceId,
                    "BlockMoved",
                    new
                    {
                        WorkspaceId = blockCtx.Page.WorkspaceId,
                        PageId = blockCtx.Block.PageId,
                        Block = response,
                        ActorUserId = currentUserId
                    });

                return ApiResult.Success(response, "Di chuyển block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResult.Fail("Block đã bị người khác cập nhật, vui lòng tải lại", "CONCURRENCY_CONFLICT");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi di chuyển block", "SERVER_ERROR", new[] { ex.Message });
            }
            finally
            {
                if (lockAcquired && currentUserId > 0)
                {
                    await BlockRealtimeHelper.ReleaseEditLockSafeAsync(
                        _presenceService, blockId, currentUserId);
                }
            }
        }

        public async Task<ApiResult> DeleteBlockAsync(int blockId, CancellationToken ct = default)
        {
            var lockAcquired = false;
            int currentUserId = 0;
            try
            {
                if (blockId <= 0)
                    return ApiResult.Fail("BlockId không hợp lệ", "INVALID_INPUT");

                var blockCtx = await BlockAccessHelper.GetBlockAccessContextAsync(
                    _dataContext, _userService, _permissionService, blockId, ct);

                currentUserId = blockCtx.UserId;

                await BlockRealtimeHelper.TouchPagePresenceAsync(
                    _presenceService, blockCtx.Page.Id, currentUserId, UserName, ct);

                await BlockRealtimeHelper.AcquireEditLockOrThrowAsync(
                    _presenceService, blockId, currentUserId, ct);
                lockAcquired = true;

                var deletedIds = await BlockTreeHelper.SoftDeleteSubtreeAsync(
                    _dataContext, blockCtx.Block, ct);

                await SaveChangesAsync(ct);

                await BlockRealtimeHelper.NotifyWorkspaceSafeAsync(
                    _notifier,
                    blockCtx.Page.WorkspaceId,
                    "BlockDeleted",
                    new
                    {
                        WorkspaceId = blockCtx.Page.WorkspaceId,
                        PageId = blockCtx.Block.PageId,
                        BlockId = blockCtx.Block.Id,
                        DeletedIds = deletedIds,
                        ActorUserId = currentUserId
                    });

                return ApiResult.Success(new
                {
                    BlockId = blockCtx.Block.Id,
                    DeletedIds = deletedIds
                }, "Xóa block thành công");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiResult.Fail(ex.Message, "FORBIDDEN");
            }
            catch (DomainException ex)
            {
                return ApiResult.Fail(ex.Message, "VALIDATION_ERROR");
            }
            catch (DbUpdateConcurrencyException)
            {
                return ApiResult.Fail("Block đã bị người khác cập nhật, vui lòng tải lại", "CONCURRENCY_CONFLICT");
            }
            catch (Exception ex)
            {
                return ApiResult.Fail("Lỗi khi xóa block", "SERVER_ERROR", new[] { ex.Message });
            }
            finally
            {
                if (lockAcquired && currentUserId > 0)
                {
                    await BlockRealtimeHelper.ReleaseEditLockSafeAsync(
                        _presenceService, blockId, currentUserId);
                }
            }
        }
    }
}