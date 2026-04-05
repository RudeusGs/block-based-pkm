namespace server.Service.Models.TaskPerformanceMetric
{
    public class UpdateMetricOnCompletionModel
    {
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public int DurationMinutes { get; set; }
    }
}
