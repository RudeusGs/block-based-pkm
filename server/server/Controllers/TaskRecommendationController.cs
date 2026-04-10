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
        public async Task<IActionResult> GenerateRecommendations(int userId, int workspaceId)
        {
            return FromApiResult(await _recommendationService.GenerateRecommendationsAsync(userId, workspaceId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/pending")]
        public async Task<IActionResult> GetPendingRecommendations(int userId, int workspaceId)
        {
            return FromApiResult(await _recommendationService.GetPendingRecommendationsAsync(userId, workspaceId));
        }

        [HttpPost("{recommendationId:int}/accept")]
        public async Task<IActionResult> AcceptRecommendation(int recommendationId)
        {
            return FromApiResult(await _recommendationService.AcceptRecommendationAsync(recommendationId));
        }

        [HttpPost("{recommendationId:int}/reject")]
        public async Task<IActionResult> RejectRecommendation(int recommendationId)
        {
            return FromApiResult(await _recommendationService.RejectRecommendationAsync(recommendationId));
        }

        [HttpPost("{recommendationId:int}/complete")]
        public async Task<IActionResult> CompleteRecommendation(int recommendationId)
        {
            return FromApiResult(await _recommendationService.CompleteRecommendationAsync(recommendationId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/history")]
        public async Task<IActionResult> GetRecommendationHistory(int userId, int workspaceId)
        {
            return FromApiResult(await _recommendationService.GetRecommendationHistoryAsync(userId, workspaceId));
        }

        [HttpGet("{recommendationId:int}")]
        public async Task<IActionResult> GetRecommendationById(int recommendationId)
        {
            return FromApiResult(await _recommendationService.GetRecommendationByIdAsync(recommendationId));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/effectiveness")]
        public async Task<IActionResult> GetRecommendationEffectiveness(int userId, int workspaceId)
        {
            return FromApiResult(await _recommendationService.GetRecommendationEffectivenessAsync(userId, workspaceId));
        }

        [HttpPost("cleanup-expired")]
        public async Task<IActionResult> CleanupExpiredRecommendations()
        {
            return FromApiResult(await _recommendationService.CleanupExpiredRecommendationsAsync());
        }

        [HttpPost("workspace/{workspaceId:int}/recalculate-weights")]
        public async Task<IActionResult> RecalculateWeights(int workspaceId)
        {
            return FromApiResult(await _recommendationService.RecalculateWeightsAsync(workspaceId));
        }

        [HttpGet("workspace/{workspaceId:int}/top")]
        public async Task<IActionResult> GetTopRecommendedTasks(int workspaceId, [FromQuery] int limit = 10)
        {
            return FromApiResult(await _recommendationService.GetTopRecommendedTasksAsync(workspaceId, limit));
        }

        [HttpGet("user/{userId:int}/highest-scoring")]
        public async Task<IActionResult> GetHighestScoringTasks(int userId, [FromQuery] int limit = 5)
        {
            return FromApiResult(await _recommendationService.GetHighestScoringTasksAsync(userId, limit));
        }
    }
}
