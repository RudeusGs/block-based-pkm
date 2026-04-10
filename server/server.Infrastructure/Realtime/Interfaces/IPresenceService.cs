namespace server.Infrastructure.Realtime.Interfaces
{
    public interface IPresenceService
    {
        Task HeartbeatPageAsync(int pageId, int userId, string userName);
        Task<List<PresenceUserModel>> GetActiveUsersOnPageAsync(int pageId);
        Task<bool> AcquireBlockLockAsync(int blockId, int userId);
        Task ReleaseBlockLockAsync(int blockId, int userId);
    }

    public class PresenceUserModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime LastActiveAt { get; set; }
    }
}
