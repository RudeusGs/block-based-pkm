using server.Domain.Base;

namespace server.Domain.Entities
{
    public class Page : EntityBase
    {
        public int WorkspaceId { get; private set; }
        public int? ParentPageId { get; private set; }

        public string Title { get; private set; }
        public string? Icon { get; private set; }
        public string? CoverImage { get; private set; }

        public int CreatedBy { get; private set; }
        public int? LastModifiedBy { get; private set; }

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

        public void UpdateTitle(string newTitle, int userId)
        {
            EnsureNotDeleted();
            EnsureNotArchived();

            SetTitle(newTitle);
            RegisterModification(userId);
        }

        public void UpdateContent(string? newContent, int userId)
        {
            EnsureNotDeleted();
            EnsureNotArchived();
            RegisterModification(userId);
        }

        public void UpdateAppearance(string? icon, string? coverImage, int userId)
        {
            EnsureNotDeleted();
            EnsureNotArchived();

            Icon = NormalizeString(icon);
            CoverImage = NormalizeString(coverImage);

            RegisterModification(userId);
        }

        public void Move(int targetWorkspaceId, int? targetParentPageId, int userId)
        {
            EnsureNotDeleted();
            EnsureNotArchived();

            if (targetWorkspaceId <= 0)
                throw new DomainException("Workspace không hợp lệ.");

            if (targetParentPageId.HasValue && targetParentPageId == Id)
                throw new DomainException("Không thể move vào chính nó.");

            if (targetParentPageId.HasValue && targetWorkspaceId != WorkspaceId)
                throw new DomainException("Không thể move sang workspace khác khi có parent.");

            WorkspaceId = targetWorkspaceId;
            ParentPageId = targetParentPageId;

            RegisterModification(userId);
        }

        public void Archive(int userId)
        {
            EnsureNotDeleted();

            if (IsArchived) return;

            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;

            foreach (var sub in SubPages)
            {
                sub.Archive(userId);
            }

            RegisterModification(userId);
        }

        public void Restore(int userId)
        {
            EnsureNotDeleted();

            if (!IsArchived) return;

            IsArchived = false;
            ArchivedAt = null;

            foreach (var sub in SubPages)
            {
                sub.Restore(userId);
            }

            RegisterModification(userId);
        }

        // PRIVATE 

        private void SetTitle(string title)
        {
            var normalized = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();

            if (normalized.Length > 200)
                throw new DomainException("Title không được vượt quá 200 ký tự.");

            Title = normalized;
        }

        private string? NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }

        private void RegisterModification(int userId)
        {
            if (userId <= 0)
                throw new DomainException("UserId không hợp lệ.");

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