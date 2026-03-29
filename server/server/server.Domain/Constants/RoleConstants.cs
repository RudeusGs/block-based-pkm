namespace server.Domain.Constants
{
    public static class RoleConstants
    {
        // Vai trò trong toàn bộ hệ thống không phải trong 1 workspace, dùng để phân quyền truy cập API.
        public const string ADMIN = "Admin"; // Quản trị viên quản lý toàn bộ hệ thống
        public const string REGULAR_USER = "User"; // User thường
    }
}
