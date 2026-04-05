using server.Domain.Enums;

namespace server.Service.Models.WorkTask
{
    public class UpdateWorkTaskModel
    {
        public int TaskId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public StatusWorkTask? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
