namespace Pkm.Application.Features.Notifications.Queries.ListNotifications;

public sealed class ListNotificationsQueryValidator
{
    public IReadOnlyList<string> Validate(ListNotificationsQuery query)
    {
        var errors = new List<string>();

        if (query.WorkspaceId == Guid.Empty)
            errors.Add("WorkspaceId không hợp lệ.");

        if (query.PageNumber <= 0)
            errors.Add("PageNumber phải lớn hơn 0.");

        if (query.PageSize <= 0 || query.PageSize > 100)
            errors.Add("PageSize phải nằm trong khoảng 1-100.");

        return errors;
    }
}