using server.Domain.Enums;

namespace server.Service.Models.WorkspaceMember
{
    public class UpdateWorkspaceMemberModel
    {
        public int WorkspaceId { get; set; }
        public int WorkspaceMemberId { get; set; }
        public RoomRole Role { get; set; }
    }
}
