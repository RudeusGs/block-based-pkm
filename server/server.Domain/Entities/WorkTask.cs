using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkTask: Thực thể đại diện cho một công việc trong hệ thống.
    /// 
    ///  Mục đích:
    /// - Quản lý dữ liệu cốt lõi của Task (title, status, priority, deadline...)
    /// - Đóng gói các hành vi chính liên quan đến vòng đời Task (Complete, ReOpen, Update)
    /// 
    ///  Design Principle:
    /// - Keep Entity simple, focused, và dễ maintain
    /// - Tránh duplicate data (Single Source of Truth)
    /// 
    ///  Flow sử dụng:
    /// - Tạo task → constructor
    /// - Cập nhật thông tin → UpdateDetails()
    /// - Hoàn thành → Complete()
    /// - Mở lại → ReOpen()
    /// </summary>
    public class WorkTask : EntityBase
    {
        /// <summary>
        /// Tiêu đề của công việc (bắt buộc)
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Mô tả chi tiết của công việc (optional)
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Trạng thái hiện tại của Task (ToDo, Done, ...)
        /// </summary>
        public StatusWorkTask Status { get; private set; }

        /// <summary>
        /// Độ ưu tiên của Task (Low, Medium, High)
        /// </summary>
        public PriorityWorkTask Priority { get; private set; }

        /// <summary>
        /// Thời hạn hoàn thành (deadline)
        /// </summary>
        public DateTime? DueDate { get; private set; }

        /// <summary>
        /// ID Workspace chứa Task
        /// </summary>
        public int WorkspaceId { get; private set; }

        /// <summary>
        /// ID Page chứa Task (có thể null nếu không thuộc page nào)
        /// </summary>
        public int? PageId { get; private set; }

        /// <summary>
        /// ID của người tạo Task
        /// </summary>
        public int CreatedById { get; private set; }

        /// <summary>
        /// Constructor phục vụ cho ORM (EF Core)
        /// </summary>
        protected WorkTask() { }

        /// <summary>
        /// Khởi tạo một Task mới
        /// </summary>
        /// <param name="title">Tiêu đề Task</param>
        /// <param name="workspaceId">Workspace chứa Task</param>
        /// <param name="createdById">Người tạo</param>
        /// <param name="pageId">Page chứa Task (optional)</param>
        /// <param name="priority">Độ ưu tiên (default: Medium)</param>
        /// <exception cref="DomainException">Nếu dữ liệu không hợp lệ</exception>
        public WorkTask(string title, int workspaceId, int createdById, int? pageId = null, PriorityWorkTask priority = PriorityWorkTask.Medium)
        {
            SetTitle(title);
            WorkspaceId = workspaceId;
            CreatedById = createdById;
            PageId = pageId;
            Priority = priority;
            Status = StatusWorkTask.ToDo;
        }

        /// <summary>
        /// Cập nhật thông tin cơ bản của Task
        /// </summary>
        /// <param name="title">Tiêu đề mới</param>
        /// <param name="description">Mô tả mới</param>
        /// <param name="priority">Độ ưu tiên mới</param>
        /// <param name="dueDate">Deadline mới</param>
        public void UpdateDetails(string title, string? description, PriorityWorkTask priority, DateTime? dueDate)
        {
            SetTitle(title);
            Description = description;
            Priority = priority;
            DueDate = dueDate;

            MarkUpdated();
        }

        /// <summary>
        /// Đánh dấu Task là đã hoàn thành
        /// </summary>
        public void Complete()
        {
            // Nếu đã hoàn thành thì bỏ qua
            if (Status == StatusWorkTask.Done) return;

            Status = StatusWorkTask.Done;
            MarkUpdated();
        }

        /// <summary>
        /// Mở lại Task (chuyển từ Done về ToDo)
        /// </summary>
        public void ReOpen()
        {
            // Nếu đang ở trạng thái ToDo thì không cần xử lý
            if (Status == StatusWorkTask.ToDo) return;

            Status = StatusWorkTask.ToDo;
            MarkUpdated();
        }

        /// <summary>
        /// Validate và set Title
        /// </summary>
        /// <param name="title">Tiêu đề</param>
        /// <exception cref="DomainException">Nếu title rỗng</exception>
        private void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề công việc không được để trống.");

            Title = title.Trim();
        }
    }
}