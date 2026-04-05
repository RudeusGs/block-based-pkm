using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskComment: Thực thể bình luận trong một công việc.
    /// </summary>
    public class TaskComment : EntityBase
    {        public int TaskId { get; private set; }
        public int UserId { get; private set; }
        public string Content { get; private set; }
        protected TaskComment() { }

        public TaskComment(int taskId, int userId, string content)
        {
            if (taskId <= 0)
                throw new DomainException("TaskId không hợp lệ.");

            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            SetContent(content);

            TaskId = taskId;
            UserId = userId;
        }
        public void UpdateContent(string newContent, int requestUserId)
        {
            if (UserId != requestUserId)
                throw new DomainException("Bạn không có quyền chỉnh sửa bình luận này.");

            SetContent(newContent);
            MarkUpdated();
        }
        private void SetContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung bình luận không được để trống.");

            if (content.Length > 2000)
                throw new DomainException("Nội dung bình luận quá dài.");

            Content = content.Trim();
        }
    }
}