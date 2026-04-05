using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkspaceMember: Quản lý thành viên trong một workspace, bao gồm vai trò và thời điểm tham gia.
    /// </summary>
    public class WorkspaceMember : EntityBase
    {
        public int WorkspaceId { get; private set; }
        public int UserId { get; private set; }
        public RoomRole Role { get; private set; }
        public DateTime JoinedAt { get; private set; }
        public virtual Workspace Workspace { get; private set; }

        protected WorkspaceMember() { }
        public WorkspaceMember(int workspaceId, int userId)
        {
            if (workspaceId <= 0) throw new DomainException("WorkspaceId không hợp lệ.");
            if (userId <= 0) throw new DomainException("UserId không hợp lệ.");
            WorkspaceId = workspaceId;
            UserId = userId;
            Role = RoomRole.RegularUser;
            JoinedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory Method dành riêng cho việc ghi đè vai trò của chủ sở hữu khi tạo thành viên mới. Chủ sở hữu sẽ luôn có vai trò GroupLeader. 
        /// </summary>
        public static WorkspaceMember CreateAsOwner(int workspaceId, int ownerId)
        {
            return new WorkspaceMember(workspaceId, ownerId)
            {
                Role = RoomRole.GroupLeader
            };
        }

        public void AssignRole(RoomRole newRole)
        {
            Role = newRole;
            MarkUpdated();
        }

        public bool IsAdmin() => Role == RoomRole.GroupLeader;
    }
}