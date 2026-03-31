using server.Domain.Base;
using server.Domain.Enums;

namespace server.Domain.Entities
{
    /// <summary>
    /// WorkspaceMember: Thành viên của một không gian làm việc.
    /// </summary>
    public class WorkspaceMember : EntityBase
    {
        /// <summary>
        /// Mã định danh của không gian làm việc.
        /// </summary>
        public int WorkspaceId { get; set; }

        /// <summary>
        /// Mã định danh của người dùng là thành viên.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Vai trò của thành viên trong workspace.
        /// </summary>
        public RoomRole Role { get; set; } = RoomRole.RegularUser;

        /// <summary>
        /// Thời điểm người dùng tham gia vào workspace.
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
