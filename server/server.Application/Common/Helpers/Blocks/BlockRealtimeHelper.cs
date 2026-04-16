using server.Domain.Base;
using server.Domain.Realtime;
using server.Service.Common.Helpers;

namespace server.Application.Common.Helpers.Blocks
{
    public static class BlockRealtimeHelper
    {
        public static async Task TouchPagePresenceAsync(
            IPresenceService presenceService,
            int pageId,
            int userId,
            string userName,
            CancellationToken ct)
        {
            await presenceService.HeartbeatPageAsync(pageId, userId, userName, ct);
        }

        public static async Task AcquireEditLockOrThrowAsync(
            IPresenceService presenceService,
            int blockId,
            int userId,
            CancellationToken ct)
        {
            var locked = await presenceService.AcquireBlockLockAsync(blockId, userId, ct);
            if (!locked)
                throw new DomainException("Block đang được người khác chỉnh sửa. Vui lòng thử lại sau.");
        }

        public static async Task ReleaseEditLockSafeAsync(
            IPresenceService presenceService,
            int blockId,
            int userId)
        {
            try
            {
                await presenceService.ReleaseBlockLockAsync(blockId, userId);
            }
            catch
            {
                // best effort
            }
        }

        public static async Task NotifyWorkspaceSafeAsync(
            IRealtimeNotifier notifier,
            int workspaceId,
            string eventName,
            object payload)
        {
            await ServiceHelper.SafeNotifyAsync(notifier, workspaceId, eventName, payload);
        }
    }
}
