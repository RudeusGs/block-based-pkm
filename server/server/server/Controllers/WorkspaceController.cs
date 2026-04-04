using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.Workspace;
using server.Service.Models;

namespace server.Controllers
{
    [Authorize]
    [Route("api/workspaces")]
    [ApiController]
    public class WorkspaceController : BaseController
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddWorkspaceModel model)
        {
            var result = await _workspaceService.CreateWorkspaceAsync(model);
            return FromApiResult(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkspaceModel model)
        {
            var result = await _workspaceService.UpdateWorkspaceAsync(model);
            return FromApiResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _workspaceService.DeleteWorkspaceAsync(id);
            return FromApiResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _workspaceService.GetWorkspaceByIdAsync(id);
            return FromApiResult(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyWorkspaces(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!this.TryGetUserId(out var userId))
                return this.FailUnauthorized();

            var paging = new PagingRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _workspaceService.GetAllByUserIdAsync(userId, paging);
            return FromApiResult(result);
        }
    }
}