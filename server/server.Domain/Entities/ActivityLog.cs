using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// ActivityLog: Thực thể nhật ký hoạt động.
    /// Dùng để theo dõi mọi thay đổi trong hệ thống (Auditing).
    /// </summary>
    public class ActivityLog : EntityBase
    {
        public int WorkspaceId { get; private set; }
        public int UserId { get; private set; }

        // Sử dụng Enum để tối ưu truy vấn và đồng nhất dữ liệu
        public ActionActivityLog Action { get; private set; }
        public EntityTypeActivityLog EntityType { get; private set; }

        // ID của thực thể bị tác động (Task ID, Page ID...)
        public int EntityId { get; private set; }

        // Mô tả ngắn gọn bằng văn bản (VD: "User A đã cập nhật trạng thái Task B")
        public string? Description { get; private set; }

        /// <summary>
        /// Dữ liệu chi tiết dạng JSON.
        /// Thường chứa: { "OldValue": {...}, "NewValue": {...}, "IpAddress": "..." }
        /// </summary>
        public string? Metadata { get; private set; }

        // Constructor protected phục vụ cho ORM (EF Core)
        protected ActivityLog() { }

        /// <summary>
        /// Khởi tạo một bản ghi nhật ký mới.
        /// Nhật ký là bất biến (Immutable) nên không có các phương thức Update.
        /// </summary>
        public ActivityLog(
            int workspaceId,
            int userId,
            ActionActivityLog action,
            EntityTypeActivityLog entityType,
            int entityId,
            string? description = null,
            string? metadata = null)
        {
            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId không hợp lệ.");

            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            if (entityId <= 0)
                throw new DomainException("EntityId của đối tượng tác động không hợp lệ.");

            WorkspaceId = workspaceId;
            UserId = userId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            Description = description?.Trim();
            Metadata = metadata;
        }
    }
}