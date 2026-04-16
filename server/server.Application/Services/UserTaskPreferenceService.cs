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

        public async Task<ApiResult> CreatePreferenceAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var existing = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (existing != null)
                    return ApiResult.Fail("Cấu hình đã tồn tại.");

                var pref = new UserTaskPreference(userId, workspaceId);
                _dataContext.UserTaskPreferences.Add(pref);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> GetPreferenceAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null)
                {
                    pref = new UserTaskPreference(userId, workspaceId);
                    _dataContext.UserTaskPreferences.Add(pref);
                    await SaveChangesAsync(ct);
                }

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdatePreferenceAsync(int userId, int workspaceId, UpdateUserTaskPreferenceModel model, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

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

                await SaveChangesAsync(ct);
                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateWorkHoursAsync(UpdateWorkHoursModel model, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == model.UserId && p.WorkspaceId == model.WorkspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.UpdateWorkHours(model.StartHour, model.EndHour);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdatePreferredDaysAsync(UpdatePreferredDaysModel model, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == model.UserId && p.WorkspaceId == model.WorkspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.SetPreferredDays(model.DaysOfWeek);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateSensitivityAsync(int userId, int workspaceId, int sensitivity, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.UpdateSensitivity(sensitivity);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateMinPriorityAsync(int userId, int workspaceId, string minPriorityStr, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                if (Enum.TryParse<PriorityWorkTask>(minPriorityStr, true, out var minPriority))
                {
                    pref.UpdateMinPriority(minPriority);
                    await SaveChangesAsync(ct);
                    return ApiResult.Success(pref);
                }

                return ApiResult.Fail("Giá trị priority không hợp lệ.");
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateRecommendationIntervalAsync(int userId, int workspaceId, int intervalMinutes, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.UpdateRecommendationInterval(intervalMinutes);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> ToggleAutoRecommendationAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.SetAutoRecommendation(!pref.EnableAutoRecommendation);
                await SaveChangesAsync(ct);

                return ApiResult.Success(new { Enabled = pref.EnableAutoRecommendation });
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> UpdateMaxRecommendationsAsync(int userId, int workspaceId, int maxCount, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.UpdateMaxRecommendations(maxCount);
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> ResetToDefaultAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                if (pref == null) return ApiResult.Fail("Không tìm thấy cấu hình.");

                pref.ResetToDefault();
                await SaveChangesAsync(ct);

                return ApiResult.Success(pref);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }

        public async Task<ApiResult> IsAutoRecommendationEnabledAsync(int userId, int workspaceId, CancellationToken ct = default)
        {
            try
            {
                var pref = await _dataContext.UserTaskPreferences
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.WorkspaceId == workspaceId, ct);

                bool isEnabled = pref?.EnableAutoRecommendation ?? false;
                return ApiResult.Success(isEnabled);
            }
            catch (OperationCanceledException) { return ApiResult.Fail("Tác vụ đã bị hủy", "CANCELED"); }
            catch (Exception ex) { return ApiResult.Fail("Lỗi hệ thống", "SERVER_ERROR", new[] { ex.Message }); }
        }
    }
}
