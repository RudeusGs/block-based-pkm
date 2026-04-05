namespace server.Infrastructure.Cache
{
    /// <summary>
    /// IRedisCacheService: Interface định nghĩa các thao tác với Redis cache.
    /// Hỗ trợ lưu trữ và truy xuất dữ liệu dạng object (JSON) và string.
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// Lưu object vào Redis (tự động serialize sang JSON)
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="key">Key định danh trong Redis</param>
        /// <param name="value">Giá trị cần lưu</param>
        /// <param name="expiry">Thời gian hết hạn (optional)</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Lấy object từ Redis (tự động deserialize từ JSON)
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="key">Key trong Redis</param>
        /// <returns>Object hoặc null nếu không tồn tại</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Lưu dữ liệu dạng chuỗi vào Redis
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Giá trị chuỗi</param>
        /// <param name="expiry">Thời gian hết hạn (optional)</param>
        Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

        /// <summary>
        /// Lấy dữ liệu dạng chuỗi từ Redis
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Chuỗi hoặc null nếu không tồn tại</returns>
        Task<string?> GetStringAsync(string key);

        /// <summary>
        /// Xóa dữ liệu khỏi Redis theo key
        /// </summary>
        /// <param name="key">Key cần xóa</param>
        Task RemoveAsync(string key);
    }
}