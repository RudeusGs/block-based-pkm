namespace Pkm.Application.Features.Recommendations.Commands.UpdateUserTaskPreference;

public sealed class UpdateUserTaskPreferenceCommandValidator
{
    public IReadOnlyList<string> Validate(UpdateUserTaskPreferenceCommand command)
    {
        var errors = new List<string>();

        if (command.WorkspaceId == Guid.Empty)
            errors.Add("WorkspaceId không hợp lệ.");

        if (command.WorkDayStartHour < 0 || command.WorkDayStartHour > 23)
            errors.Add("WorkDayStartHour phải trong khoảng 0-23.");

        if (command.WorkDayEndHour < 0 || command.WorkDayEndHour > 23)
            errors.Add("WorkDayEndHour phải trong khoảng 0-23.");

        if (command.WorkDayStartHour >= command.WorkDayEndHour)
            errors.Add("WorkDayStartHour phải nhỏ hơn WorkDayEndHour.");

        if (command.PreferredDaysOfWeek.Count == 0)
            errors.Add("PreferredDaysOfWeek không được rỗng.");

        if (command.PreferredDaysOfWeek.Any(x => x < 0 || x > 6))
            errors.Add("PreferredDaysOfWeek chỉ nhận giá trị 0-6.");

        if (command.MaxRecommendationsPerSession <= 0 || command.MaxRecommendationsPerSession > 20)
            errors.Add("MaxRecommendationsPerSession phải trong khoảng 1-20.");

        if (command.RecommendationSensitivity < 0 || command.RecommendationSensitivity > 100)
            errors.Add("RecommendationSensitivity phải trong khoảng 0-100.");

        if (command.RecommendationIntervalMinutes <= 0 || command.RecommendationIntervalMinutes > 1440)
            errors.Add("RecommendationIntervalMinutes phải trong khoảng 1-1440.");

        return errors;
    }
}