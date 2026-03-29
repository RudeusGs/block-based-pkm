using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.Workspace;
using server.Service.Common.IServices;

namespace server.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceController : BaseController
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IUserService _userService;

        public WorkspaceController(IWorkspaceService workspaceService, IUserService userService)
        {
            _workspaceService = workspaceService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddWorkspaceModel model, CancellationToken ct)
        {
            var result = await _workspaceService.CreateWorkspaceAsync(model);
            return FromApiResult(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateWorkspaceModel model, CancellationToken ct)
        {
            var result = await _workspaceService.UpdateWorkspaceAsync(model);
            return FromApiResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _workspaceService.DeleteWorkspaceAsync(id);
            return FromApiResult(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyWorkspaces(CancellationToken ct)
        {
            var result = await _workspaceService.GetAllByUserIdAsync(_userService.UserId);
            return FromApiResult(result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _workspaceService.GetWorkspaceByIdAsync(id);
            return FromApiResult(result);
        }
    }
}