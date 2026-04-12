namespace server.Domain.Realtime;

public interface IPresenceService
{
    Task HeartbeatPageAsync(int pageId, int userId, string userName, CancellationToken ct = default);

    Task<List<PresenceUserModel>> GetActiveUsersOnPageAsync(int pageId, CancellationToken ct = default);

    Task<bool> AcquireBlockLockAsync(int blockId, int userId, CancellationToken ct = default);

    Task ReleaseBlockLockAsync(int blockId, int userId, CancellationToken ct = default);
}

public class PresenceUserModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime LastActiveAt { get; set; }
}
