using server.Domain.Enums;
using server.Service.Models;
using server.Service.Models.WorkspaceMember;

namespace server.Service.Interfaces
{
    public interface IWorkspaceMemberService
    {
        Task<ApiResult> AddMemberAsync(AddWorkspaceMemberModel model, CancellationToken ct = default);
        Task<ApiResult> UpdateMemberRoleAsync(int workspaceId, int userId, RoomRole role, CancellationToken ct = default);
        Task<ApiResult> RemoveMemberAsync(int workspaceId, int userId, CancellationToken ct = default);
        Task<ApiResult> GetWorkspaceMembersAsync(int workspaceId, CancellationToken ct = default);

    }
}
