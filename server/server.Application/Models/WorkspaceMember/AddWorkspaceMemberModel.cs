using server.Domain.Enums;

namespace server.Service.Models.WorkspaceMember
{
    public class AddWorkspaceMemberModel
    {
        public int WorkspaceId { get; set; }
        public string UserEmail { get; set; }
        public RoomRole Role { get; set; } = RoomRole.RegularUser;
    }
}
