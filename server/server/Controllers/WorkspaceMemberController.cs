using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain.Enums;
using server.Service.Interfaces;
using server.Service.Models.WorkspaceMember;

namespace server.Controllers
{
    [Authorize]
    [Route("api/workspace-members")]
    [ApiController]
    public class WorkspaceMemberController : BaseController
    {
        private readonly IWorkspaceMemberService _workspaceMemberService;

        public WorkspaceMemberController(IWorkspaceMemberService workspaceMemberService)
        {
            _workspaceMemberService = workspaceMemberService;
        }

        /// <summary>
        /// Thêm thành viên vào workspace
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddMember([FromBody] AddWorkspaceMemberModel model, CancellationToken ct)
        {
            var result = await _workspaceMemberService.AddMemberAsync(model, ct);
            return FromApiResult(result);
        }

        /// <summary>
        /// Lấy danh sách thành viên của workspace
        /// </summary>
        [HttpGet("{workspaceId:int}")]
        public async Task<IActionResult> GetMembers(int workspaceId, CancellationToken ct)
        {
            var result = await _workspaceMemberService.GetWorkspaceMembersAsync(workspaceId, ct);
            return FromApiResult(result);
        }

        /// <summary>
        /// Xóa thành viên khỏi workspace
        /// </summary>
        [HttpDelete("{workspaceId:int}/users/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int workspaceId, int userId, CancellationToken ct)
        {
            var result = await _workspaceMemberService.RemoveMemberAsync(workspaceId, userId, ct);
            return FromApiResult(result);
        }

        /// <summary>
        /// Cập nhật role thành viên
        /// </summary>
        [HttpPut("{workspaceId:int}/users/{userId:int}")]
        public async Task<IActionResult> UpdateRole(int workspaceId, int userId, [FromBody] RoomRole role, CancellationToken ct)
        {
            var result = await _workspaceMemberService.UpdateMemberRoleAsync(workspaceId, userId, role, ct);
            return FromApiResult(result);
        }
    }
}