using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Service.Interfaces;
using server.Service.Models.UserTaskPreference;

namespace server.Controllers
{
    [Route("api/user-task-preferences")]
    [Authorize]
    [ApiController]
    public class UserTaskPreferenceController : BaseController
    {
        private readonly IUserTaskPreferenceService _preferenceService;

        public UserTaskPreferenceController(IUserTaskPreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
        }

        [HttpPost("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> CreatePreference(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.CreatePreferenceAsync(userId, workspaceId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetPreference(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.GetPreferenceAsync(userId, workspaceId, ct));
        }

        [HttpPut("user/{userId:int}/workspace/{workspaceId:int}")]
        public async Task<IActionResult> UpdatePreference(int userId, int workspaceId, [FromBody] UpdateUserTaskPreferenceModel model, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdatePreferenceAsync(userId, workspaceId, model, ct));
        }

        [HttpPut("work-hours")]
        public async Task<IActionResult> UpdateWorkHours([FromBody] UpdateWorkHoursModel model, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdateWorkHoursAsync(model, ct));
        }

        [HttpPut("preferred-days")]
        public async Task<IActionResult> UpdatePreferredDays([FromBody] UpdatePreferredDaysModel model, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdatePreferredDaysAsync(model, ct));
        }

        [HttpPut("user/{userId:int}/workspace/{workspaceId:int}/sensitivity")]
        public async Task<IActionResult> UpdateSensitivity(int userId, int workspaceId, [FromBody] int sensitivity, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdateSensitivityAsync(userId, workspaceId, sensitivity, ct));
        }

        [HttpPut("user/{userId:int}/workspace/{workspaceId:int}/min-priority")]
        public async Task<IActionResult> UpdateMinPriority(int userId, int workspaceId, [FromBody] string minPriority, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdateMinPriorityAsync(userId, workspaceId, minPriority, ct));
        }

        [HttpPut("user/{userId:int}/workspace/{workspaceId:int}/interval")]
        public async Task<IActionResult> UpdateRecommendationInterval(int userId, int workspaceId, [FromBody] int intervalMinutes, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdateRecommendationIntervalAsync(userId, workspaceId, intervalMinutes, ct));
        }

        [HttpPost("user/{userId:int}/workspace/{workspaceId:int}/toggle-auto")]
        public async Task<IActionResult> ToggleAutoRecommendation(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.ToggleAutoRecommendationAsync(userId, workspaceId, ct));
        }

        [HttpPut("user/{userId:int}/workspace/{workspaceId:int}/max-recommendations")]
        public async Task<IActionResult> UpdateMaxRecommendations(int userId, int workspaceId, [FromBody] int maxCount, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.UpdateMaxRecommendationsAsync(userId, workspaceId, maxCount, ct));
        }

        [HttpPost("user/{userId:int}/workspace/{workspaceId:int}/reset")]
        public async Task<IActionResult> ResetToDefault(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.ResetToDefaultAsync(userId, workspaceId, ct));
        }

        [HttpGet("user/{userId:int}/workspace/{workspaceId:int}/is-auto-enabled")]
        public async Task<IActionResult> IsAutoRecommendationEnabled(int userId, int workspaceId, CancellationToken ct)
        {
            return FromApiResult(await _preferenceService.IsAutoRecommendationEnabledAsync(userId, workspaceId, ct));
        }
    }
}
