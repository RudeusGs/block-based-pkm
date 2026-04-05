using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkTask: Thực thể công việc với logic tự động tính toán trọng số gợi ý.
    /// </summary>
    public class WorkTask : EntityBase
    {
        // Dữ liệu cơ bản
        public string Title { get; private set; }
        public string? Description { get; private set; }
        public StatusWorkTask Status { get; private set; }
        public PriorityWorkTask Priority { get; private set; }
        public DateTime? DueDate { get; private set; }
        public int WorkspaceId { get; private set; }
        public int? PageId { get; private set; }
        public int CreatedById { get; private set; }

        // Dữ liệu gợi ý & Thống kê
        // Số lần task này đã được hoàn thành, dùng để đánh giá mức độ quan trọng và tần suất xuất hiện trong gợi ý.
        public int CompletionCount { get; private set; }
        // Thời điểm task được hoàn thành lần cuối, dùng để đánh giá tính thời sự và ưu tiên trong gợi ý.
        public DateTime? LastCompletedAt { get; private set; }
        // Tổng thời gian (phút) đã được ghi nhận khi hoàn thành task, dùng để đánh giá độ phức tạp và thời gian cần thiết cho gợi ý.
        public int TotalDurationMinutes { get; private set; }
        // Trọng số gợi ý được tính toán dựa trên các yếu tố như độ ưu tiên, tần suất hoàn thành, thời gian hoàn thành, ... Dùng để xếp hạng trong thuật toán gợi ý.
        public decimal RecommendationWeight { get; private set; }
        // Giờ trong ngày mà task này thường được hoàn thành (0-23), dùng để gợi ý vào những thời điểm phù hợp trong ngày.
        public int? OptimalHourOfDay { get; private set; }
        // Chuỗi lịch sử hoàn thành gần đây, JSON chứa các timestamp, dùng để phân tích thói quen hoàn thành và cải thiện thuật toán gợi ý.
        public string? RecentCompletionHistory { get; private set; }

        protected WorkTask() { }

        public WorkTask(string title, int workspaceId, int createdById, int? pageId = null, PriorityWorkTask priority = PriorityWorkTask.Medium)
        {
            SetTitle(title);
            WorkspaceId = workspaceId;
            CreatedById = createdById;
            PageId = pageId;
            Priority = priority;
            Status = StatusWorkTask.ToDo;

            // Khởi tạo các giá trị mặc định
            CompletionCount = 0;
            TotalDurationMinutes = 0;
            RecommendationWeight = 0;
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết của Task.
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
        /// Hoàn thành task và tự động cập nhật các chỉ số thông minh.
        /// </summary>
        public void Complete(int durationMinutes)
        {
            if (Status == StatusWorkTask.Done) return;

            Status = StatusWorkTask.Done;
            LastCompletedAt = DateTime.UtcNow;
            CompletionCount++;
            TotalDurationMinutes += Math.Max(0, durationMinutes);

            // Cập nhật giờ tối ưu dựa trên thời điểm hoàn thành thực tế
            UpdateOptimalHour(DateTime.UtcNow.Hour);

            // Tự động tính toán lại trọng số cho thuật toán gợi ý
            CalculateWeight();

            MarkUpdated();
        }

        public void ReOpen()
        {
            if (Status == StatusWorkTask.ToDo) return;
            Status = StatusWorkTask.ToDo;

            // Khi mở lại, có thể giảm nhẹ trọng số hoặc giữ nguyên tùy nghiệp vụ
            MarkUpdated();
        }

        private void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Tiêu đề công việc không được để trống.");

            Title = title.Trim();
        }

        private void UpdateOptimalHour(int currentHour)
        {
            if (OptimalHourOfDay == null)
            {
                OptimalHourOfDay = currentHour;
            }
            else
            {
                OptimalHourOfDay = (OptimalHourOfDay.Value + currentHour) / 2;
            }
        }

        private void CalculateWeight()
        {
            decimal priorityScore = Priority switch
            {
                PriorityWorkTask.High => 3.0m,
                PriorityWorkTask.Medium => 2.0m,
                PriorityWorkTask.Low => 1.0m,
                _ => 1.0m
            };
            RecommendationWeight = (CompletionCount * 0.7m) + (priorityScore * 1.5m);
        }

        /// <summary>
        /// Cập nhật chuỗi lịch sử (thường là kết quả của việc Serialize một List object)
        /// </summary>
        public void UpdateHistory(string jsonHistory)
        {
            RecentCompletionHistory = jsonHistory;
            MarkUpdated();
        }
    }
}