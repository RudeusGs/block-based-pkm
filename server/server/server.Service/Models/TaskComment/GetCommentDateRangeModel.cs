namespace server.Service.Models.TaskComment
{
    public class GetCommentDateRangeModel
    {
        public int TaskId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
