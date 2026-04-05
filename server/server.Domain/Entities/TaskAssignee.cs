using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskAssignee: Người được giao thực hiện một công việc.
    /// </summary>
    public class TaskAssignee : EntityBase
    {
        /// <summary>
        /// Mã định danh của công việc.
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Mã định danh của người được giao thực hiện công việc.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Thời điểm thực hiện việc giao bài.
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
