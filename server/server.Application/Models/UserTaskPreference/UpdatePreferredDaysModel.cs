namespace server.Service.Models.UserTaskPreference
{
    public class UpdatePreferredDaysModel
    {
        public int UserId { get; set; }
        public int WorkspaceId { get; set; }
        public List<int> DaysOfWeek { get; set; }
    }
}
