namespace server.Service.Interfaces.Authentication
{
    /// <summary>
    /// IRoleManagementService: Quản lý vai trò của người dùng.
    /// </summary>
    public interface IRoleManagementService
    {
        /// <summary>
        /// Đảm bảo vai trò người dùng thường tồn tại, nếu không thì tạo mới.
        /// </summary>
        Task EnsureRegularRoleExistsAsync();

        /// <summary>
        /// Gán vai trò người dùng thường cho người dùng.
        /// </summary>
        Task<(bool succeeded, List<string> errors)> AddToRegularRoleAsync(int userId);
    }
}