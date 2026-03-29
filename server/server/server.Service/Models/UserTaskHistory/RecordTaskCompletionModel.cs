namespace server.Service.Models.UserTaskHistory
{
    public class RecordTaskCompletionModel
    {
        public int TaskId { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
    }
}
