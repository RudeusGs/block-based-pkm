using server.Domain.Enums;

namespace server.Service.Models.WorkTask
{
    public class UpdateWorkTaskModel
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public StatusWorkTask? Status { get; set; }
        public PriorityWorkTask? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
