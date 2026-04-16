namespace server.Service.Models.UserTaskHistory
{
    public class GetHistoryDateRangeModel
    {
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
