namespace server.Service.Models.WorkTask
{
    public class AddWorkTaskModel
    {
        public int WorkspaceId { get; set; }
        public int? PageId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
