using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskComment: Bình luận trên một công việc.
    /// </summary>
    public class TaskComment : EntityBase
    {
        /// <summary>
        /// Mã định danh của công việc.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Mã định danh của người viết bình luận.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Nội dung bình luận.
        /// </summary>
        public string Content { get; set; }
    }
}
