using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models.WorkspaceMember;

namespace server.Controllers
{
    [Authorize]
    [Route("api/workspace-members")]
    public class WorkspaceMemberController : BaseController
    {
        private readonly IWorkspaceMemberService _workspaceMemberService;
        public WorkspaceMemberController(
            IWorkspaceMemberService workspaceMemberService)
        {
            _workspaceMemberService = workspaceMemberService;
        }

        /// <summary>
        /// Thêm thành viên vào workspace
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddMember([FromBody] AddWorkspaceMemberModel model, CancellationToken ct)
        {
            var result = await _workspaceMemberService.AddMemberAsync(model);
            return FromApiResult(result);
        }

        /// <summary>
        /// Lấy danh sách thành viên của workspace
        /// </summary>
        [HttpGet("{workspaceId}")]
        public async Task<IActionResult> GetMembers(int workspaceId, CancellationToken ct)
        {
            var result = await _workspaceMemberService.GetWorkspaceMembersAsync(workspaceId);
            return FromApiResult(result);
        }

        /// <summary>
        /// Xóa thành viên khỏi workspace
        /// </summary>
        [HttpDelete("{workspaceId}/user/{userId}")]
        public async Task<IActionResult> RemoveMember(int workspaceId, int userId, CancellationToken ct)
        {
            var result = await _workspaceMemberService.RemoveMemberAsync(workspaceId, userId);
            return FromApiResult(result);
        }

        /// <summary>
        /// Cập nhật role thành viên
        /// </summary>
        [HttpPut("role")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateWorkspaceMemberModel model, CancellationToken ct)
        {
            var result = await _workspaceMemberService.UpdateMemberRoleAsync(model);
            return FromApiResult(result);
        }
    }
}