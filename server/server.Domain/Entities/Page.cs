using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Page: Thực thể trang tài liệu.
    /// Hỗ trợ cấu trúc phân cấp (Parent/Child), quản lý nội dung và Xóa mềm (Archive).
    /// </summary>
    public class Page : EntityBase
    {
        public int WorkspaceId { get; private set; }
        public int? ParentPageId { get; private set; }
        public string Title { get; private set; }
        public string? Content { get; private set; }
        public string? Icon { get; private set; }
        public string? CoverImage { get; private set; }

        public int CreatedBy { get; private set; }
        public int? LastModifiedBy { get; private set; }

        // Logic Xóa mềm
        public bool IsArchived { get; private set; }
        public DateTime? ArchivedAt { get; private set; }

        // Navigation Properties (Phục vụ cấu trúc cây của Page)
        public virtual Page? ParentPage { get; private set; }
        public virtual ICollection<Page> SubPages { get; private set; } = new List<Page>();
        public virtual ICollection<WorkTask> Tasks { get; private set; } = new List<WorkTask>();

        protected Page() { }

        public Page(string title, int workspaceId, int createdBy, int? parentPageId = null)
        {
            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId phải lớn hơn 0.");

            if (createdBy <= 0)
                throw new DomainException("Người tạo (CreatedBy) không hợp lệ.");

            WorkspaceId = workspaceId;
            CreatedBy = createdBy;
            ParentPageId = parentPageId;
            IsArchived = false;

            SetTitle(title);
        }

        public void UpdateTitle(string newTitle, int userId)
        {
            EnsureNotArchived();
            SetTitle(newTitle);
            RegisterModification(userId);
        }

        public void UpdateContent(string? newContent, int userId)
        {
            EnsureNotArchived();
            Content = newContent;
            RegisterModification(userId);
        }

        public void UpdateAppearance(string? icon, string? coverImage, int userId)
        {
            EnsureNotArchived();
            Icon = icon;
            CoverImage = coverImage;
            RegisterModification(userId);
        }

        public void Move(int targetWorkspaceId, int? targetParentPageId, int userId)
        {
            EnsureNotArchived();
            if (targetWorkspaceId <= 0)
                throw new DomainException("Workspace đích không hợp lệ.");

            if (targetParentPageId.HasValue && targetParentPageId == Id)
                throw new DomainException("Không thể di chuyển trang vào chính nó.");

            WorkspaceId = targetWorkspaceId;
            ParentPageId = targetParentPageId;
            RegisterModification(userId);
        }

        /// <summary>
        /// Đưa trang vào thùng rác
        /// </summary>
        public void Archive(int userId)
        {
            if (IsArchived) return;

            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;
            RegisterModification(userId);
        }

        /// <summary>
        /// Khôi phục trang từ thùng rác.
        /// </summary>
        public void Restore(int userId)
        {
            if (!IsArchived) return;

            IsArchived = false;
            ArchivedAt = null;
            RegisterModification(userId);
        }

        private void SetTitle(string title)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();
            if (Title.Length > 200)
                throw new DomainException("Tiêu đề trang không được quá 200 ký tự.");
        }

        private void RegisterModification(int userId)
        {
            LastModifiedBy = userId;
            MarkUpdated();
        }

        private void EnsureNotArchived()
        {
            if (IsArchived)
                throw new DomainException("Không thể chỉnh sửa trang đã được lưu trữ (Archived). Hãy khôi phục trang trước.");
        }
    }
}