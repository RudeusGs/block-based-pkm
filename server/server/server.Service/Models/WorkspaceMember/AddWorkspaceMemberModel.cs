namespace server.Service.Models.WorkspaceMember
{
    public class AddWorkspaceMemberModel
    {
        public int WorkspaceId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
    }
}
