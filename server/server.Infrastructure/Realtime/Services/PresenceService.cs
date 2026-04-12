using System.Text.Json;
using server.Domain.Realtime;
using StackExchange.Redis;

namespace server.Infrastructure.Realtime.Services
{
    public class PresenceService : IPresenceService
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _presenceTimeout = TimeSpan.FromSeconds(30);

        public PresenceService(IConnectionMultiplexer connection)
        {
            _db = connection.GetDatabase();
        }

        public async Task HeartbeatPageAsync(int pageId, int userId, string userName, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string userKey = $"Presence:Page:{pageId}:User:{userId}";

            var userPresence = new PresenceUserModel
            {
                UserId = userId,
                UserName = userName,
                LastActiveAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(userPresence);

            await _db.StringSetAsync(userKey, json, _presenceTimeout);
        }

        public async Task<List<PresenceUserModel>> GetActiveUsersOnPageAsync(int pageId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string matchPattern = $"Presence:Page:{pageId}:User:*";

            var connection = _db.Multiplexer;
            var endpoints = connection.GetEndPoints();
            var server = connection.GetServer(endpoints.First());
            var keys = server.Keys(pattern: matchPattern).ToArray();

            var users = new List<PresenceUserModel>();

            if (keys.Any())
            {
                var values = await _db.StringGetAsync(keys);
                foreach (var val in values)
                {
                    ct.ThrowIfCancellationRequested();
                    if (val.HasValue)
                    {
                        var user = JsonSerializer.Deserialize<PresenceUserModel>(val.ToString()!);
                        if (user != null) users.Add(user);
                    }
                }
            }

            return users;
        }

        public async Task<bool> AcquireBlockLockAsync(int blockId, int userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string key = $"Lock:Block:{blockId}";
            bool acquired = await _db.StringSetAsync(key, userId.ToString(), TimeSpan.FromSeconds(30), When.NotExists);

            if (!acquired)
            {
                var lockedBy = await _db.StringGetAsync(key);
                if (lockedBy.HasValue && lockedBy.ToString() == userId.ToString())
                {
                    await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(30));
                    return true;
                }
            }

            return acquired;
        }

        public async Task ReleaseBlockLockAsync(int blockId, int userId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string key = $"Lock:Block:{blockId}";
            var lockedBy = await _db.StringGetAsync(key);

            if (lockedBy.HasValue && lockedBy.ToString() == userId.ToString())
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
