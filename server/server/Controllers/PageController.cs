using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.Page;

namespace server.Controllers
{
    [Authorize]
    [Route("api/pages")]
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
            if (!this.TryGetUserId(out var userId))
                return this.FailUnauthorized();

            var result = await _pageService.CreatePageAsync(model, userId);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{pageId:int}")] 
        public async Task<IActionResult> Update(int pageId, [FromBody] UpdatePageModel model)
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
        public async Task<IActionResult> GetByWorkspace(int workspaceId, [FromQuery] string? q = null)
        {
            var keyword = q ?? string.Empty;
            var result = string.IsNullOrEmpty(keyword)
                ? await _pageService.GetPagesByWorkspaceAsync(workspaceId)
                : await _pageService.SearchPagesAsync(workspaceId, keyword);

            return FromApiResult(result);
        }
    }
}