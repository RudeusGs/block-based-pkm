namespace server.Domain.Enums
{
    public enum EntityTypeActivityLog
    {
        Workspace = 1,       // Không gian làm việc
        WorkspaceMember = 2, // Thành viên trong không gian
        Page = 3,            // Trang tài liệu
        WorkTask = 4,        // Công việc/Nhiệm vụ
        TaskComment = 5,     // Bình luận trong công việc
        TaskAssignee = 6,    // Người thực hiện công việc
        User = 7,            // Thông tin người dùng (Profile)
        UserPreference = 8,  // Cấu hình cá nhân hóa
        RealtimeSession = 9  // Phiên làm việc (Dành cho log bảo mật/truy cập)
    }
}
