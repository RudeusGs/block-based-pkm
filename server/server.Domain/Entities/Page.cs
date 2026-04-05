using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// Page: Thực thể trang tài liệu. 
    /// </summary>
    public class Page : EntityBase
    {        
        public int WorkspaceId { get; private set; }
        public string Title { get; private set; }
        public int CreatedBy { get; private set; }
        protected Page() { }
        public Page(string title, int workspaceId, int createdBy)
        {
            if (workspaceId <= 0)
                throw new DomainException("WorkspaceId phải lớn hơn 0.");

            if (createdBy <= 0)
                throw new DomainException("Người tạo (CreatedBy) không hợp lệ.");

            WorkspaceId = workspaceId;
            CreatedBy = createdBy;
            SetTitle(title);
        }

        public void UpdateTitle(string newTitle)
        {
            SetTitle(newTitle);
            MarkUpdated();
        }
        public void MoveToWorkspace(int targetWorkspaceId)
        {
            if (targetWorkspaceId <= 0)
                throw new DomainException("Workspace đích không hợp lệ.");

            WorkspaceId = targetWorkspaceId;
            MarkUpdated();
        }
        private void SetTitle(string title)
        {
            // Tiêu đề trống sẽ được đặt thành "Untitled".
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();
        }
    }
}