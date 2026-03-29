namespace server.Service.Models.WorkTask
{
    public class UpdateWorkTaskModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } // To Do, Doing, Done
        public string? Priority { get; set; } // Low, Medium, High
        public DateTime? DueDate { get; set; }
    }
}
