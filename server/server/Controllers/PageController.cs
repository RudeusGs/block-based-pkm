using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.Page;

namespace server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/pages")]
    public class PageController : BaseController
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddPageModel model, CancellationToken ct)
        {
            var result = await _pageService.CreatePageAsync(model, ct);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpPut("{pageId:int}")]
        public async Task<IActionResult> Update(int pageId, [FromBody] UpdatePageModel model, CancellationToken ct)
        {
            var result = await _pageService.UpdatePageAsync(pageId, model, ct);
            return FromApiResult(result);
        }

        [HttpDelete("{pageId:int}")]
        public async Task<IActionResult> Delete(int pageId, CancellationToken ct)
        {
            var result = await _pageService.DeletePageAsync(pageId, ct);
            return FromApiResult(result);
        }

        [HttpGet("{pageId:int}")]
        public async Task<IActionResult> GetById(int pageId, CancellationToken ct)
        {
            var result = await _pageService.GetPageByIdAsync(pageId, ct);
            return FromApiResult(result);
        }

        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetByWorkspace(
            int workspaceId,
            [FromQuery] string? q,
            [FromQuery] PagingRequest? paging,
            CancellationToken ct = default)
        {
            var effectivePaging = paging ?? new PagingRequest { PageNumber = 1, PageSize = 10 };

            var result = string.IsNullOrWhiteSpace(q)
                ? await _pageService.GetPagesByWorkspaceAsync(workspaceId, effectivePaging, ct)
                : await _pageService.SearchPagesAsync(workspaceId, q, effectivePaging, ct);

            return FromApiResult(result);
        }

        [HttpGet("{pageId:int}/sub-pages")]
        public async Task<IActionResult> GetSubPages(int pageId, CancellationToken ct)
        {
            var result = await _pageService.GetSubPagesAsync(pageId, ct);
            return FromApiResult(result);
        }
    }
}