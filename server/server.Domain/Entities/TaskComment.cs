using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskComment: Thực thể bình luận trong Task.
    /// 
    ///  Mục đích:
    /// - Quản lý comment và reply (threaded comment)
    /// - Hỗ trợ soft delete (không mất dữ liệu)
    /// 
    ///  Lưu ý:
    /// - Comment có thể có parent → tạo thành cây
    /// - Parent phải cùng Task
    /// - Không cho phép reply vào comment đã bị xóa
    /// 
    ///  Design Principle:
    /// - Preserve data (không mất content thật)
    /// - Validate chặt chẽ quan hệ cha-con
    /// </summary>
    public class TaskComment : EntityBase
    {
        // Định danh
        public int TaskId { get; private set; }
        public int UserId { get; private set; }

        /// <summary>
        /// Comment cha (null nếu root)
        /// </summary>
        public int? ParentId { get; private set; }

        // Nội dung
        public string Content { get; private set; }

        /// <summary>
        /// Lưu content gốc để phục vụ restore/audit
        /// </summary>
        public string? OriginalContent { get; private set; }

        // Trạng thái
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Navigation
        public virtual TaskComment? Parent { get; private set; }
        public virtual ICollection<TaskComment> Replies { get; private set; } = new List<TaskComment>();

        protected TaskComment() { }

        /// <summary>
        /// Tạo comment mới hoặc reply
        /// </summary>
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
            IsDeleted = false;

            SetContent(content);
        }

        /// <summary>
        /// Cập nhật nội dung
        /// </summary>
        public void UpdateContent(string newContent, int requestUserId)
        {
            EnsureEditable(requestUserId);

            SetContent(newContent);
            MarkUpdated();
        }

        /// <summary>
        /// Xóa mềm comment (không mất dữ liệu thật)
        /// </summary>
        public void SoftDelete(int requestUserId)
        {
            EnsureEditable(requestUserId);

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;

            // Lưu lại content gốc
            OriginalContent = Content;

            // Nội dung hiển thị
            Content = "Bình luận này đã bị gỡ bỏ.";

            MarkUpdated();
        }

        /// <summary>
        /// Khôi phục comment
        /// </summary>
        public void Restore(int requestUserId)
        {
            if (!IsDeleted)
                return;

            if (UserId != requestUserId)
                throw new DomainException("Bạn không có quyền khôi phục.");

            IsDeleted = false;
            DeletedAt = null;

            // Khôi phục content
            if (!string.IsNullOrWhiteSpace(OriginalContent))
                Content = OriginalContent;

            OriginalContent = null;

            MarkUpdated();
        }

        private void SetContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung không được để trống.");

            if (content.Length > 2000)
                throw new DomainException("Nội dung tối đa 2000 ký tự.");

            Content = content.Trim();
        }

        private void EnsureEditable(int requestUserId)
        {
            if (IsDeleted)
                throw new DomainException("Không thể chỉnh sửa comment đã bị xóa.");

            if (UserId != requestUserId)
                throw new DomainException("Không có quyền thao tác.");
        }
    }
}