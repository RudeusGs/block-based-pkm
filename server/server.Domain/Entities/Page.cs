using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Page: Trang tài liệu trong một không gian làm việc.
    /// </summary>
    public class Page : EntityBase
    {
        /// <summary>
        /// Mã định danh của không gian làm việc.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Tiêu đề của trang.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Mã định danh của người tạo ra trang.
        /// </summary>
        public int CreatedBy { get; set; }
    }
}
