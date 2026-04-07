using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkTask: Thực thể công việc tập trung vào quản lý trạng thái và dữ liệu cốt lõi.
    /// Các chỉ số hiệu suất chi tiết được chuyển sang TaskPerformanceMetric để tránh dư thừa.
    /// </summary>
    public class WorkTask : EntityBase
    {
        // --- Dữ liệu cơ bản ---
        public string Title { get; private set; }
        public string? Description { get; private set; }
        public StatusWorkTask Status { get; private set; }
        public PriorityWorkTask Priority { get; private set; }
        public DateTime? DueDate { get; private set; }
        public int WorkspaceId { get; private set; }
        public int? PageId { get; private set; }
        public int CreatedById { get; private set; }

        // --- Dữ liệu hỗ trợ gợi ý nhanh (Source of Truth cho hiển thị) ---
        public int CompletionCount { get; private set; }
        public DateTime? LastCompletedAt { get; private set; }
        public int? OptimalHourOfDay { get; private set; }
        public decimal RecommendationWeight { get; private set; }

        protected WorkTask() { }

        public WorkTask(string title, int workspaceId, int createdById, int? pageId = null, PriorityWorkTask priority = PriorityWorkTask.Medium)
        {
            SetTitle(title);
            WorkspaceId = workspaceId;
            CreatedById = createdById;
            PageId = pageId;
            Priority = priority;
            Status = StatusWorkTask.ToDo;

            CompletionCount = 0;
            RecommendationWeight = 0;
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết. Trọng số sẽ được tính lại dựa trên Priority mới.
        /// </summary>
        public void UpdateDetails(string title, string? description, PriorityWorkTask priority, DateTime? dueDate)
        {
            SetTitle(title);
            Description = description;
            Priority = priority;
            DueDate = dueDate;

            CalculateWeight();
            MarkUpdated();
        }

        /// <summary>
        /// Hoàn thành task. Cập nhật các chỉ số cơ bản phục vụ gợi ý nhanh.
        /// </summary>
        public void Complete()
        {
            if (Status == StatusWorkTask.Done) return;

            Status = StatusWorkTask.Done;
            LastCompletedAt = DateTime.UtcNow;
            CompletionCount++;

            // Cập nhật giờ tối ưu theo công thức Cumulative Moving Average
            UpdateOptimalHour(DateTime.UtcNow.Hour);

            // Cập nhật trọng số ưu tiên
            CalculateWeight();

            MarkUpdated();
        }

        public void ReOpen()
        {
            if (Status == StatusWorkTask.ToDo) return;
            Status = StatusWorkTask.ToDo;
            MarkUpdated();
        }

        private void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề công việc không được để trống.");

            Title = title.Trim();
        }

        /// <summary>
        /// Tính toán giờ người dùng thường làm task này nhất bằng công thức trung bình tích lũy.
        /// Giúp dữ liệu ổn định, không bị nhiễu bởi các lần hoàn thành bất thường.
        /// </summary>
        private void UpdateOptimalHour(int currentHour)
        {
            if (!OptimalHourOfDay.HasValue)
            {
                OptimalHourOfDay = currentHour;
            }
            else
            {
                // Công thức: NewAvg = ((OldAvg * (N-1)) + NewVal) / N
                double updatedHour = ((double)OptimalHourOfDay.Value * (CompletionCount - 1) + currentHour) / CompletionCount;
                OptimalHourOfDay = (int)Math.Round(updatedHour);
            }
        }

        /// <summary>
        /// Trọng số này dùng để thuật toán AI ưu tiên các task quan trọng 
        /// hoặc các task thường xuyên được thực hiện trong quá khứ.
        /// </summary>
        private void CalculateWeight()
        {
            decimal priorityScore = Priority switch
            {
                PriorityWorkTask.High => 3.0m,
                PriorityWorkTask.Medium => 2.0m,
                PriorityWorkTask.Low => 1.0m,
                _ => 1.0m
            };

            // Logic: Ưu tiên Độ ưu tiên (1.5) cao hơn Tần suất hoàn thành (0.7)
            RecommendationWeight = (CompletionCount * 0.7m) + (priorityScore * 1.5m);
        }
    }
}