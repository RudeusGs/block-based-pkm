using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;

namespace server.Controllers
{
    [Route("api/task-recommendations")]
    [Authorize]
    [ApiController]
    public class TaskRecommendationController : BaseController
    {
        private readonly ITaskRecommendationService _recommendationService;

        public TaskRecommendationController(ITaskRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpPost("user/{userId:int}/workspace/{workspaceId:int}/generate")]
        public async Task<IActionResult> GenerateRecommendations(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.GenerateRecommendationsAsync(userId, workspaceId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/pending")]
        public async Task<IActionResult> GetPendingRecommendations(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.GetPendingRecommendationsAsync(userId, workspaceId, ct));
        }

        [HttpPost("{recommendationId:int}/accept")]
        public async Task<IActionResult> AcceptRecommendation(int recommendationId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.AcceptRecommendationAsync(recommendationId, ct));
        }

        [HttpPost("{recommendationId:int}/reject")]
        public async Task<IActionResult> RejectRecommendation(int recommendationId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.RejectRecommendationAsync(recommendationId, ct));
        }

        [HttpPost("{recommendationId:int}/complete")]
        public async Task<IActionResult> CompleteRecommendation(int recommendationId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.CompleteRecommendationAsync(recommendationId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/history")]
        public async Task<IActionResult> GetRecommendationHistory(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.GetRecommendationHistoryAsync(userId, workspaceId, ct));
        }

        [HttpGet("{recommendationId:int}")]
        public async Task<IActionResult> GetRecommendationById(int recommendationId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.GetRecommendationByIdAsync(recommendationId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/effectiveness")]
        public async Task<IActionResult> GetRecommendationEffectiveness(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.GetRecommendationEffectivenessAsync(userId, workspaceId, ct));
        }

        [HttpPost("cleanup-expired")]
        public async Task<IActionResult> CleanupExpiredRecommendations(CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.CleanupExpiredRecommendationsAsync(ct));
        }

        [HttpPost("workspace/{workspaceId:int}/recalculate-weights")]
        public async Task<IActionResult> RecalculateWeights(int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _recommendationService.RecalculateWeightsAsync(workspaceId, ct));
        }

        [HttpGet("workspace/{workspaceId:int}/top")]
        public async Task<IActionResult> GetTopRecommendedTasks(int workspaceId, [FromQuery] int limit = 10, CancellationToken ct = default)
        {
            return FromApiResult(await _recommendationService.GetTopRecommendedTasksAsync(workspaceId, limit, ct));
        }

        [HttpGet("user/{userId:int}/highest-scoring")]
        public async Task<IActionResult> GetHighestScoringTasks(int userId, [FromQuery] int limit = 5, CancellationToken ct = default)
        {
            return FromApiResult(await _recommendationService.GetHighestScoringTasksAsync(userId, limit, ct));
        }
    }
}
