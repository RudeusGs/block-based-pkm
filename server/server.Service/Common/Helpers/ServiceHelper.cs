using server.Infrastructure.Realtime.Interfaces;
using server.Service.Common.IServices;

namespace server.Service.Common.Helpers
{
    public static class ServiceHelper
    {
        public static int GetCurrentUserIdOrThrow(IUserService userService)
        {
            var userId = userService.UserId;

            if (userId <= 0)
                throw new UnauthorizedAccessException("Unauthorized");

            return userId;
        }

        public static async Task SafeNotifyAsync(
            IRealtimeNotifier notifier,
            int workspaceId,
            string eventName,
            object payload)
        {
            try
            {
                await notifier.SendToWorkspaceAsync(workspaceId, eventName, payload);
            }
            catch (Exception)
            {
            }
        }
    }
}