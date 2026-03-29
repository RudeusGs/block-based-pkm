using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// ActivityLog: Nhật ký hoạt động trong hệ thống.
    /// Ghi lại các hành động: tạo, chỉnh sửa, xóa trên các đối tượng.
    /// </summary>
    public class ActivityLog : EntityBase
    {
        /// <summary>
        /// Mã định danh của không gian làm việc.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Mã định danh của người thực hiện hành động.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Loại hành động (Create, Update, Delete, ...).
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Loại đối tượng bị tác động (Task, Page, Workspace, ...).
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Mã định danh của đối tượng bị tác động.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Dữ liệu bổ sung dưới dạng JSON (giá trị cũ, giá trị mới, ...).
        /// </summary>
        public string? Metadata { get; set; }
    }
}
