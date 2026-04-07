using server.Domain.Base;

namespace server.Domain.Entities
{
    /// <summary>
    /// TaskPerformanceMetric: Thực thể lưu trữ dữ liệu thô về hiệu suất của Task theo từng User.
    /// 
    ///  Mục đích:
    /// - Là "Source of Truth" cho các dữ liệu tracking cơ bản (completion, abandonment).
    /// - KHÔNG chứa logic phân tích, thống kê hay AI.
    /// 
    ///  Flow chuẩn:
    /// - Khi user hoàn thành task:
    ///     → Gọi RecordCompletion()
    /// - Khi user bỏ task:
    ///     → Gọi RecordAbandonment()
    /// - Các service bên ngoài sẽ dùng dữ liệu này để:
    ///     → Tính toán analytics
    ///     → Generate recommendation
    /// 
    ///  Design Principle:
    /// - Keep Entity "dumb but safe"
    /// - Business intelligence belongs to Services, NOT Entity
    /// </summary>
    public class TaskPerformanceMetric : EntityBase
    {
        /// <summary>
        /// ID của Task liên quan
        /// </summary>
        public int TaskId { get; private set; }

        /// <summary>
        /// ID của User thực hiện Task
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// ID của Workspace chứa Task
        /// </summary>
        public int WorkspaceId { get; private set; }

        /// <summary>
        /// Số lần hoàn thành Task
        /// </summary>
        public int CompletionCount { get; private set; }

        /// <summary>
        /// Số lần bỏ dở Task
        /// </summary>
        public int AbandonedCount { get; private set; }

        /// <summary>
        /// Thời điểm hoàn thành gần nhất
        /// </summary>
        public DateTime? LastCompletedAt { get; private set; }

        /// <summary>
        /// Constructor dành cho ORM (EF Core)
        /// </summary>
        protected TaskPerformanceMetric() { }

        /// <summary>
        /// Khởi tạo metric cho một Task-User trong Workspace
        /// </summary>
        /// <param name="taskId">ID Task</param>
        /// <param name="userId">ID User</param>
        /// <param name="workspaceId">ID Workspace</param>
        /// <exception cref="DomainException">Nếu ID không hợp lệ</exception>
        public TaskPerformanceMetric(int taskId, int userId, int workspaceId)
        {
            if (taskId <= 0 || userId <= 0 || workspaceId <= 0)
                throw new DomainException("Invalid identifiers.");

            TaskId = taskId;
            UserId = userId;
            WorkspaceId = workspaceId;
        }

        /// <summary>
        /// Ghi nhận một lần hoàn thành Task
        /// </summary>
        /// <param name="completedAt">Thời điểm hoàn thành</param>
        /// <exception cref="DomainException">Nếu thời gian không hợp lệ</exception>
        public void RecordCompletion(DateTime completedAt)
        {
            // Không cho phép ghi nhận thời gian trong tương lai
            if (completedAt > DateTime.UtcNow)
                throw new DomainException("Invalid completion time.");

            CompletionCount++;
            LastCompletedAt = completedAt;

            MarkUpdated();
        }

        /// <summary>
        /// Ghi nhận một lần user bỏ dở Task
        /// </summary>
        public void RecordAbandonment()
        {
            AbandonedCount++;
            MarkUpdated();
        }
    }
}