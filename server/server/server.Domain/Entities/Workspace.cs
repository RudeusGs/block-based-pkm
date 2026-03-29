using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Workspace: Không gian làm việc của người dùng/team.
    /// </summary>
    public class Workspace : EntityBase
    {
        /// <summary>
        /// Tên của không gian làm việc.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Mô tả chi tiết về mục đích của không gian.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Mã định danh của người sở hữu/tạo ra workspace.
        /// </summary>
        public int OwnerId { get; set; }
    }
}
