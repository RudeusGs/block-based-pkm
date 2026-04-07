namespace server.Domain.Enums
{
    public enum ActionActivityLog
    {
        // Nhóm cơ bản
        Create = 1,
        Update = 2,
        Delete = 3,

        // Nhóm quản lý trạng thái
        Archive = 4,   // Đưa vào thùng rác / Lưu trữ
        Restore = 5,   // Khôi phục từ thùng rác

        // Nhóm tương tác đặc biệt
        Move = 6,      // Di chuyển trang/task sang Workspace hoặc Parent khác
        Assign = 7,    // Giao việc cho người khác
        Unassign = 8,  // Hủy giao việc

        // Nhóm hoàn thành
        Complete = 9,
        Reopen = 10,

        // Nhóm bảo mật/Hệ thống
        Login = 11,
        ChangePermissions = 12 // Thay đổi quyền thành viên
    }
}