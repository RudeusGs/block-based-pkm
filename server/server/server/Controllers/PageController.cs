using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.Page;

namespace server.Controllers
{
    [Authorize]
    [Route("api/page")]
    public class PageController : BaseController
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddPageModel model)
        {
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return FailResult("Unauthorized", StatusCodes.Status401Unauthorized, "UNAUTHORIZED");

            var result = await _pageService.CreatePageAsync(model, userId);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePageModel model)
        {
            var result = await _pageService.UpdatePageAsync(model);
            return FromApiResult(result);
        }

        [HttpDelete("{pageId:int}")]
        public async Task<IActionResult> Delete(int pageId)
        {
            var result = await _pageService.DeletePageAsync(pageId);
            return FromApiResult(result);
        }

        [HttpGet("{pageId:int}")]
        public async Task<IActionResult> GetById(int pageId)
        {
            var result = await _pageService.GetPageByIdAsync(pageId);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetByWorkspace(int workspaceId)
        {
            var result = await _pageService.GetPagesByWorkspaceAsync(workspaceId);
            return FromApiResult(result);
        }

        [HttpGet("search/{workspaceId:int}")]
        public async Task<IActionResult> Search(int workspaceId, [FromQuery] string? q)
        {
            var keyword = q ?? string.Empty;
            var result = await _pageService.SearchPagesAsync(workspaceId, keyword);
            return FromApiResult(result);
        }
    }
}
