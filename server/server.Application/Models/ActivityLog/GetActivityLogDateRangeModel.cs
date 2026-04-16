namespace server.Service.Models.ActivityLog
{
    public class GetActivityLogDateRangeModel
    {
        public int WorkspaceId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
