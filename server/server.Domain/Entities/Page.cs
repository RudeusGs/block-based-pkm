using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Page: Thực thể đại diện cho một trang tài liệu (giống Notion).
    /// 
    ///  Mục đích:
    /// - Quản lý nội dung (title, content, icon, cover)
    /// - Hỗ trợ cấu trúc cây (parent/child)
    /// - Liên kết với Task
    /// 
    ///  Lưu ý quan trọng:
    /// - Page có thể nằm trong cây phân cấp nhiều cấp
    /// - Phải đảm bảo không tạo vòng lặp (cycle)
    /// - Không cho phép chỉnh sửa khi đã archive
    /// 
    ///  Design Principle:
    /// - Page là Aggregate Root của Page Tree
    /// - Quản lý consistency của subtree
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

        // Soft delete
        public bool IsArchived { get; private set; }
        public DateTime? ArchivedAt { get; private set; }

        // Navigation
        public virtual Page? ParentPage { get; private set; }
        public virtual ICollection<Page> SubPages { get; private set; } = new List<Page>();
        public virtual ICollection<WorkTask> Tasks { get; private set; } = new List<WorkTask>();

        protected Page() { }

        public Page(string title, int workspaceId, int createdBy, int? parentPageId = null)
        {
            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId không hợp lệ.");

            if (createdBy <= 0)
                throw new DomainException("CreatedBy không hợp lệ.");

            WorkspaceId = workspaceId;
            CreatedBy = createdBy;
            ParentPageId = parentPageId;
            IsArchived = false;

            SetTitle(title);
        }

        /// <summary>
        /// Cập nhật tiêu đề
        /// </summary>
        public void UpdateTitle(string newTitle, int userId)
        {
            EnsureNotArchived();
            SetTitle(newTitle);
            RegisterModification(userId);
        }

        /// <summary>
        /// Cập nhật nội dung
        /// </summary>
        public void UpdateContent(string? newContent, int userId)
        {
            EnsureNotArchived();
            Content = newContent;
            RegisterModification(userId);
        }

        /// <summary>
        /// Cập nhật icon + cover
        /// </summary>
        public void UpdateAppearance(string? icon, string? coverImage, int userId)
        {
            EnsureNotArchived();
            Icon = icon;
            CoverImage = coverImage;
            RegisterModification(userId);
        }

        /// <summary>
        /// Di chuyển page sang workspace hoặc parent khác
        /// </summary>
        public void Move(int targetWorkspaceId, int? targetParentPageId, int userId)
        {
            EnsureNotArchived();

            if (targetWorkspaceId <= 0)
                throw new DomainException("Workspace không hợp lệ.");

            if (targetParentPageId.HasValue && targetParentPageId == Id)
                throw new DomainException("Không thể move vào chính nó.");

            // ⚠️ Không cho phép move sang workspace khác nếu có parent
            if (targetParentPageId.HasValue && targetWorkspaceId != WorkspaceId)
                throw new DomainException("Không thể move sang workspace khác khi có parent.");

            WorkspaceId = targetWorkspaceId;
            ParentPageId = targetParentPageId;

            RegisterModification(userId);
        }

        /// <summary>
        /// Archive page (soft delete)
        /// </summary>
        public void Archive(int userId)
        {
            if (IsArchived) return;

            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;

            // ⚠️ Cascade archive cho subpages
            foreach (var sub in SubPages)
            {
                sub.Archive(userId);
            }

            RegisterModification(userId);
        }

        /// <summary>
        /// Restore page
        /// </summary>
        public void Restore(int userId)
        {
            if (!IsArchived) return;

            IsArchived = false;
            ArchivedAt = null;

            // ⚠️ Cascade restore
            foreach (var sub in SubPages)
            {
                sub.Restore(userId);
            }

            RegisterModification(userId);
        }

        /// <summary>
        /// Validate & set title
        /// </summary>
        private void SetTitle(string title)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();

            if (Title.Length > 200)
                throw new DomainException("Title không được vượt quá 200 ký tự.");
        }

        private void RegisterModification(int userId)
        {
            LastModifiedBy = userId;
            MarkUpdated();
        }

        private void EnsureNotArchived()
        {
            if (IsArchived)
                throw new DomainException("Không thể chỉnh sửa page đã archive.");
        }
    }
}