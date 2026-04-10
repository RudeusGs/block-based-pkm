using StackExchange.Redis;
using System.Text.Json;
using server.Infrastructure.Realtime.Interfaces;

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

        public async Task HeartbeatPageAsync(int pageId, int userId, string userName)
        {
            string key = $"Presence:Page:{pageId}";
            
            var userPresence = new PresenceUserModel
            {
                UserId = userId,
                UserName = userName,
                LastActiveAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(userPresence);
            
            // Dùng sorted set hoặc đơn giản là set string expire theo user.
            // Để tự động dọn dẹp, ta lưu mỗi user 1 key với TTL 30s. Bạn cũng có thể dùng Hash nhưng dọn dẹp Hash phức tạp hơn do ko cài TTL từng Field được.
            string userKey = $"Presence:Page:{pageId}:User:{userId}";
            await _db.StringSetAsync(userKey, json, _presenceTimeout);
        }

        public async Task<List<PresenceUserModel>> GetActiveUsersOnPageAsync(int pageId)
        {
            // Mẫu pattern match "Presence:Page:{pageId}:User:*"
            string matchPattern = $"Presence:Page:{pageId}:User:*";
            
            // Note: Trong Redis thực tế, KEYS command không được khuyến khích dùng trên production lớn.
            // Tuy nhiên, đối với ứng dụng này hoặc với thư viện StackExchange.Redis, ta có cách lấy chuẩn là dựa vào GetServer() hoặc thiết kế Sets thay thế.
            // Vì quy mô SignalR Local/Docker, ta tạm gọi thủ công cấu trúc Set để gom:
            // Thay vì dùng pattern scan, ta query dựa trên logic đơn giản hoăc bỏ dấu * cho server local:
            
            var connection = _db.Multiplexer; // Lấy connection gốc
            var endpoints = connection.GetEndPoints();
            var server = connection.GetServer(endpoints.First());
            var keys = server.Keys(pattern: matchPattern).ToArray();

            var users = new List<PresenceUserModel>();
            
            if (keys.Any())
            {
                var values = await _db.StringGetAsync(keys);
                foreach (var val in values)
                {
                    if (val.HasValue)
                    {
                        var user = JsonSerializer.Deserialize<PresenceUserModel>(val.ToString()!);
                        if (user != null) users.Add(user);
                    }
                }
            }

            return users;
        }

        public async Task<bool> AcquireBlockLockAsync(int blockId, int userId)
        {
            string key = $"Lock:Block:{blockId}";
            // Cố gắng SET. Chỉ thành công khi key chưa tồn tại (Lock độc quyền)
            // Khóa tự động rớt sau 30s phòng trường hợp user rớt mạng không nhả lock
            bool acquired = await _db.StringSetAsync(key, userId.ToString(), TimeSpan.FromSeconds(30), When.NotExists);
            
            if (!acquired)
            {
                // Nếu đã bị khóa bởi ai đó, kiểm tra xem có phải CHÍNH MÌNH (userId này) khóa không?
                // Nếu chính mình thì gia hạn khóa.
                var lockedBy = await _db.StringGetAsync(key);
                if (lockedBy.HasValue && lockedBy.ToString() == userId.ToString())
                {
                    await _db.KeyExpireAsync(key, TimeSpan.FromSeconds(30));
                    return true;
                }
            }

            return acquired;
        }

        public async Task ReleaseBlockLockAsync(int blockId, int userId)
        {
            string key = $"Lock:Block:{blockId}";
            var lockedBy = await _db.StringGetAsync(key);
            
            // Chỉ ai khóa thì người đó mới được phép nhả khóa
            if (lockedBy.HasValue && lockedBy.ToString() == userId.ToString())
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
