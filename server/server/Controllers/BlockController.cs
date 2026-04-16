using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Application.Interfaces;
using server.Controllers;
using server.Service.Models.Block;

namespace server.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/blocks")]
    public class BlockController : BaseController
    {
        private readonly IBlockService _blockService;

        public BlockController(IBlockService blockService)
        {
            _blockService = blockService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddBlockModel model, CancellationToken ct)
        {
            var result = await _blockService.CreateBlockAsync(model, ct);
            return FromApiResult(result, StatusCodes.Status201Created);
        }

        [HttpGet("{blockId:int}")]
        public async Task<IActionResult> GetById(int blockId, CancellationToken ct)
        {
            var result = await _blockService.GetByIdAsync(blockId, ct);
            return FromApiResult(result);
        }

        [HttpGet("page/{pageId:int}")]
        public async Task<IActionResult> GetByPage(int pageId, CancellationToken ct)
        {
            var result = await _blockService.GetByPageAsync(pageId, ct);
            return FromApiResult(result);
        }

        [HttpPut("{blockId:int}")]
        public async Task<IActionResult> Update(int blockId, [FromBody] UpdateBlockModel model, CancellationToken ct)
        {
            var result = await _blockService.UpdateBlockAsync(blockId, model, ct);
            return FromApiResult(result);
        }

        [HttpPut("{blockId:int}/move")]
        public async Task<IActionResult> Move(int blockId, [FromBody] MoveBlockModel model, CancellationToken ct)
        {
            var result = await _blockService.MoveBlockAsync(blockId, model, ct);
            return FromApiResult(result);
        }

        [HttpDelete("{blockId:int}")]
        public async Task<IActionResult> Delete(int blockId, CancellationToken ct)
        {
            var result = await _blockService.DeleteBlockAsync(blockId, ct);
            return FromApiResult(result);
        }
    }
}
