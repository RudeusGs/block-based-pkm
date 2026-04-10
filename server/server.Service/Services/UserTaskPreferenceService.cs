using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;
using server.Infrastructure.Persistence;
using server.Service.Common.IServices;
using server.Service.Interfaces;
using server.Service.Models;
using server.Service.Models.UserTaskPreference;

namespace server.Service.Services
{
    public class UserTaskPreferenceService : BaseService, IUserTaskPreferenceService
    {
        public UserTaskPreferenceService(DataContext dataContext, IUserService userService)
            : base(dataContext, userService)
        {
        }

        public async Task<ApiResult> CreatePreferenceAsync(int userId, int workspaceId)
        {
            var existing = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (existing != null)
                return ApiResult.Fail("Cấu hình đã tồn tại.");

            var pref = new UserTaskPreference(userId, workspaceId);
            _dataContext.UserTaskPreferences.Add(pref);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> GetPreferenceAsync(int userId, int workspaceId)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null)
            {
                // Tự động tạo mặc định nếu chưa có
                pref = new UserTaskPreference(userId, workspaceId);
                _dataContext.UserTaskPreferences.Add(pref);
                await SaveChangesAsync();
            }

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> UpdatePreferenceAsync(int userId, int workspaceId, UpdateUserTaskPreferenceModel model)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.UpdateWorkHours(model.WorkDayStartHour, model.WorkDayEndHour);
            pref.SetPreferredDays(model.PreferredDaysOfWeek);
            pref.UpdateSensitivity(model.RecommendationSensitivity);
            pref.UpdateMaxRecommendations(model.MaxRecommendationsPerSession);
            pref.UpdateRecommendationInterval(model.RecommendationIntervalMinutes);
            pref.SetAutoRecommendation(model.EnableAutoRecommendation);

            if (Enum.TryParse<PriorityWorkTask>(model.MinPriorityForRecommendation, true, out var minPriority))
            {
                pref.UpdateMinPriority(minPriority);
            }

            await SaveChangesAsync();
            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> UpdateWorkHoursAsync(UpdateWorkHoursModel model)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == model.UserId && p.WorkspaceId == model.WorkspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.UpdateWorkHours(model.StartHour, model.EndHour);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> UpdatePreferredDaysAsync(UpdatePreferredDaysModel model)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == model.UserId && p.WorkspaceId == model.WorkspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.SetPreferredDays(model.DaysOfWeek);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> UpdateSensitivityAsync(int userId, int workspaceId, int sensitivity)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.UpdateSensitivity(sensitivity);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> UpdateMinPriorityAsync(int userId, int workspaceId, string minPriorityStr)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            if (Enum.TryParse<PriorityWorkTask>(minPriorityStr, true, out var minPriority))
            {
                pref.UpdateMinPriority(minPriority);
                await SaveChangesAsync();
                return ApiResult.Success(pref);
            }

            return ApiResult.Fail("Giá trị priority không hợp lệ.");
        }

        public async Task<ApiResult> UpdateRecommendationIntervalAsync(int userId, int workspaceId, int intervalMinutes)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.UpdateRecommendationInterval(intervalMinutes);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> ToggleAutoRecommendationAsync(int userId, int workspaceId)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.SetAutoRecommendation(!pref.EnableAutoRecommendation);
            await SaveChangesAsync();

            return ApiResult.Success(new { Enabled = pref.EnableAutoRecommendation });
        }

        public async Task<ApiResult> UpdateMaxRecommendationsAsync(int userId, int workspaceId, int maxCount)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.UpdateMaxRecommendations(maxCount);
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> ResetToDefaultAsync(int userId, int workspaceId)
        {
            var pref = await _dataContext.UserTaskPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

            pref.ResetToDefault();
            await SaveChangesAsync();

            return ApiResult.Success(pref);
        }

        public async Task<ApiResult> IsAutoRecommendationEnabledAsync(int userId, int workspaceId)
        {
            var pref = await _dataContext.UserTaskPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId);

            bool isEnabled = pref?.EnableAutoRecommendation ?? false;
            return ApiResult.Success(isEnabled);
        }
    }
}
