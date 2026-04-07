using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskComment: Thực thể bình luận trong một công việc.
    /// Hỗ trợ bình luận phân tầng (Threaded) và xóa mềm (Soft Delete) để bảo vệ dữ liệu.
    /// </summary>
    public class TaskComment : EntityBase
    {
        // Định danh và Quan hệ
        public int TaskId { get; private set; }
        public int UserId { get; private set; }

        /// <summary>
        /// ID của bình luận cha. Nếu null, đây là bình luận gốc (Root).
        /// </summary>
        public int? ParentId { get; private set; }

        // Nội dung
        public string Content { get; private set; }

        // Trạng thái Xóa mềm
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Navigation Properties
        public virtual TaskComment? Parent { get; private set; }
        public virtual ICollection<TaskComment> Replies { get; private set; } = new List<TaskComment>();

        protected TaskComment() { }

        /// <summary>
        /// Khởi tạo một bình luận mới hoặc một phản hồi.
        /// </summary>
        public TaskComment(int taskId, int userId, string content, int? parentId = null)
        {
            if (taskId <= 0) throw new DomainException("TaskId không hợp lệ.");
            if (userId <= 0) throw new DomainException("UserId không hợp lệ.");

            TaskId = taskId;
            UserId = userId;
            ParentId = parentId;
            IsDeleted = false;

            SetContent(content);
        }

        /// <summary>
        /// Cập nhật nội dung bình luận. 
        /// Không cho phép sửa nếu bình luận đã bị đánh dấu xóa.
        /// </summary>
        public void UpdateContent(string newContent, int requestUserId)
        {
            if (IsDeleted)
                throw new DomainException("Không thể chỉnh sửa bình luận đã bị xóa.");

            if (UserId != requestUserId)
                throw new DomainException("Chỉ chủ nhân mới có quyền chỉnh sửa bình luận này.");

            SetContent(newContent);
            MarkUpdated();
        }

        /// <summary>
        /// Thực hiện xóa mềm bình luận.
        /// Giữ lại bản ghi trong DB nhưng thay đổi nội dung hiển thị để bảo vệ tính riêng tư.
        /// </summary>
        public void SoftDelete(int requestUserId)
        {
            if (IsDeleted) return;

            if (UserId != requestUserId)
                throw new DomainException("Bạn không có quyền xóa bình luận này.");

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            Content = "Bình luận này đã bị gỡ bỏ.";

            MarkUpdated();
        }

        /// <summary>
        /// Khôi phục bình luận đã xóa (Tùy chọn nếu cần tính năng Undo).
        /// </summary>
        public void Restore(string originalContent)
        {
            if (!IsDeleted) return;

            IsDeleted = false;
            DeletedAt = null;
            SetContent(originalContent);

            MarkUpdated();
        }

        private void SetContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Nội dung bình luận không được để trống.");

            if (content.Length > 2000)
                throw new DomainException("Nội dung quá dài (tối đa 2000 ký tự).");

            Content = content.Trim();
        }
    }
}