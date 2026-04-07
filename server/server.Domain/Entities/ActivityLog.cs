using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// ActivityLog: Thực thể ghi nhận lịch sử hoạt động trong hệ thống (Audit Log).
    /// 
    ///  Mục đích:
    /// - Theo dõi mọi thay đổi quan trọng (create, update, delete, complete...)
    /// - Phục vụ debug, audit, và hiển thị lịch sử cho user
    /// 
    ///  Lưu ý quan trọng:
    /// - ActivityLog là IMMUTABLE (không được update sau khi tạo)
    /// - Chỉ được tạo mới, không chỉnh sửa
    /// 
    ///  Design Principle:
    /// - Write-only entity
    /// - Không chứa business logic phức tạp
    /// - Dữ liệu phải rõ ràng, dễ query
    /// </summary>
    public class ActivityLog : EntityBase
    {
        /// <summary>
        /// Workspace nơi hành động xảy ra
        /// </summary>
        public int WorkspaceId { get; private set; }

        /// <summary>
        /// User thực hiện hành động
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Loại hành động (Create, Update, Delete, Complete...)
        /// </summary>
        public ActionActivityLog Action { get; private set; }

        /// <summary>
        /// Loại thực thể bị tác động (Task, Page, Workspace...)
        /// </summary>
        public EntityTypeActivityLog EntityType { get; private set; }

        /// <summary>
        /// ID của entity bị tác động
        /// </summary>
        public int EntityId { get; private set; }

        /// <summary>
        /// Mô tả ngắn gọn (dùng để hiển thị UI)
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Dữ liệu chi tiết dạng JSON (Old/New value, extra info...)
        /// </summary>
        public string? Metadata { get; private set; }

        /// <summary>
        /// Địa chỉ IP của user (phục vụ audit)
        /// </summary>
        public string? IpAddress { get; private set; }

        /// <summary>
        /// Thời điểm xảy ra hành động (explicit cho audit)
        /// </summary>
        public DateTime OccurredAt { get; private set; }

        protected ActivityLog() { }

        /// <summary>
        /// Khởi tạo một ActivityLog mới
        /// </summary>
        public ActivityLog(
            int workspaceId,
            int userId,
            ActionActivityLog action,
            EntityTypeActivityLog entityType,
            int entityId,
            string? description = null,
            string? metadata = null,
            string? ipAddress = null)
        {
            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId không hợp lệ.");

            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            if (entityId <= 0)
                throw new DomainException("EntityId không hợp lệ.");

            WorkspaceId = workspaceId;
            UserId = userId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;

            Description = NormalizeDescription(description);
            Metadata = metadata;

            IpAddress = ipAddress;
            OccurredAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Chuẩn hóa và validate Description
        /// </summary>
        private string? NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            var trimmed = description.Trim();

            if (trimmed.Length > 500)
                throw new DomainException("Description không được vượt quá 500 ký tự.");

            return trimmed;
        }
    }
}