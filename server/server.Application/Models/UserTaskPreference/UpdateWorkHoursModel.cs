namespace server.Service.Models.UserTaskPreference
{
    public class UpdateWorkHoursModel
    {
        public int UserId { get; set; }
        public int WorkspaceId { get; set; }
        public int StartHour { get; set; }
        public int EndHour { get; set; }
    }
}
