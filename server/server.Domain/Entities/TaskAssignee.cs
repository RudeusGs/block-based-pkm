using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskAssignee: Thực thể thể hiện việc giao việc cho một thành viên.
    /// </summary>
    public class TaskAssignee : EntityBase
    {
        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public DateTime AssignedAt { get; private set; }
        protected TaskAssignee() { }

        public TaskAssignee(int taskId, int userId)
        {
            if (taskId <= 0)
                throw new DomainException("TaskId không hợp lệ.");

            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            TaskId = taskId;
            UserId = userId;
            AssignedAt = DateTime.UtcNow;
        }

    }
}