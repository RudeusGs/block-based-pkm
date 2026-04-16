namespace server.Service.Models.UserTaskHistory
{
    public class RecordAbandonmentModel
    {
        public int TaskId { get; set; }
        public int DurationMinutes { get; set; }
        public string? Reason { get; set; }
    }
}
