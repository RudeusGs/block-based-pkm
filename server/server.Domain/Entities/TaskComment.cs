using server.Domain.Base;

namespace server.Domain.Entities
{
    public class TaskComment : EntityBase
    {
        private const string DeletedMessage = "Bình luận này đã bị gỡ bỏ.";

        public int TaskId { get; private set; }
        public int UserId { get; private set; }

        public int? ParentId { get; private set; }

        public string Content { get; private set; }
        public string? OriginalContent { get; private set; }

        // Navigation
        public virtual TaskComment? Parent { get; private set; }
        public virtual ICollection<TaskComment> Replies { get; private set; } = new List<TaskComment>();

        protected TaskComment() { }

        public TaskComment(int taskId, int userId, string content, TaskComment? parent = null)
        {
            if (taskId <= 0)
                throw new DomainException("TaskId không hợp lệ.");

            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

            if (parent != null)
            {
                if (parent.TaskId != taskId)
                    throw new DomainException("Parent phải cùng Task.");

                if (parent.IsDeleted)
                    throw new DomainException("Không thể reply vào comment đã bị xóa.");

                Parent = parent;
                ParentId = parent.Id;
            }

            TaskId = taskId;
            UserId = userId;

            SetContent(content);
        }

        public void UpdateContent(string newContent, int requestUserId)
        {
            EnsureNotDeleted();
            EnsureOwner(requestUserId);

            SetContent(newContent);
            MarkUpdated();
        }

        public void SoftDelete(int requestUserId)
        {
            EnsureNotDeleted();
            EnsureOwner(requestUserId);

            if (OriginalContent == null)
                OriginalContent = Content;

            Content = DeletedMessage;

            base.SoftDelete();
        }

        public void Restore(int requestUserId)
        {
            if (!IsDeleted)
                return;

            EnsureOwner(requestUserId);

            base.Restore();

            if (!string.IsNullOrWhiteSpace(OriginalContent))
                Content = OriginalContent;

            OriginalContent = null;
        }

        // PRIVATE

        private void SetContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung không được để trống.");

            var trimmed = content.Trim();

            if (trimmed.Length > 2000)
                throw new DomainException("Nội dung tối đa 2000 ký tự.");

            Content = trimmed;
        }

        private void EnsureOwner(int userId)
        {
            if (UserId != userId)
                throw new DomainException("Không có quyền thao tác.");
        }
    }
}