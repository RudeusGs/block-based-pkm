using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace server.Infrastructure.Cache
{
    /// <summary>
    /// RedisCacheService: Service dùng để thao tác với Redis cache.
    /// Hỗ trợ lưu, lấy và xóa dữ liệu dạng string hoặc object (serialize JSON).
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        /// <summary>
        /// Khởi tạo RedisCacheService
        /// </summary>
        /// <param name="connection">Kết nối Redis</param>
        /// <param name="logger">Logger để ghi log lỗi</param>
        public RedisCacheService(IConnectionMultiplexer connection, ILogger<RedisCacheService> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _db = _connection.GetDatabase();
            _logger = logger;
        }

        /// <summary>
        /// Lưu object vào Redis dưới dạng JSON
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="key">Key trong Redis</param>
        /// <param name="value">Giá trị cần lưu</param>
        /// <param name="expiry">Thời gian hết hạn (optional)</param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, json);

                if (expiry.HasValue)
                {
                    await _db.KeyExpireAsync(key, expiry.Value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi Redis SetAsync với key: {Key}", key);
            }
        }

        /// <summary>
        /// Lấy object từ Redis và deserialize từ JSON
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="key">Key trong Redis</param>
        /// <returns>Object hoặc null nếu không tồn tại</returns>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue) return default;

                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi Redis GetAsync với key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Lưu chuỗi vào Redis
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Giá trị chuỗi</param>
        /// <param name="expiry">Thời gian hết hạn (optional)</param>
        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                await _db.StringSetAsync(key, value);

                if (expiry.HasValue)
                {
                    await _db.KeyExpireAsync(key, expiry.Value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi Redis SetStringAsync với key: {Key}", key);
            }
        }

        /// <summary>
        /// Lấy chuỗi từ Redis
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Chuỗi hoặc null nếu không tồn tại</returns>
        public async Task<string?> GetStringAsync(string key)
        {
            try
            {
                var val = await _db.StringGetAsync(key);
                return val.HasValue ? val.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi Redis GetStringAsync với key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Xóa key khỏi Redis
        /// </summary>
        /// <param name="key">Key cần xóa</param>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi Redis RemoveAsync với key: {Key}", key);
            }
        }
    }
}