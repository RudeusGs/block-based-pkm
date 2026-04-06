using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// ActivityLog: Thực thể nhật ký hoạt động.
    /// </summary>
    public class ActivityLog : EntityBase
    {
        public int WorkspaceId { get; private set; }
        public int UserId { get; private set; }
        public string Action { get; private set; } // Create, Update, Delete...
        public string EntityType { get; private set; } // Task, Page, Workspace...
        public int EntityId { get; private set; }
        public string? Metadata { get; private set; } // JSON chứa OldValue/NewValue

        protected ActivityLog() { }

        /// <summary>
        /// Khởi tạo một bản ghi nhật ký mới.
        /// </summary>
        public ActivityLog(
            int workspaceId,
            int userId,
            string action,
            string entityType,
            int entityId,
            string? metadata = null)
        {
            if (workspaceId <= 0) throw new DomainException("WorkspaceId không hợp lệ.");
            if (userId <= 0) throw new DomainException("UserId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(action))
                throw new DomainException("Hành động (Action) không được để trống.");

            if (string.IsNullOrWhiteSpace(entityType))
                throw new DomainException("Loại đối tượng (EntityType) không được để trống.");

            WorkspaceId = workspaceId;
            UserId = userId;
            Action = action.Trim().ToUpper();
            EntityType = entityType.Trim();
            EntityId = entityId;
            Metadata = metadata;
        }
    }
}