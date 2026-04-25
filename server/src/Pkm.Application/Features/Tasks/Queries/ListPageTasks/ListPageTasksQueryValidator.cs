namespace Pkm.Application.Features.Tasks.Queries.ListPageTasks;

public sealed class ListPageTasksQueryValidator
{
    public IReadOnlyList<string> Validate(ListPageTasksQuery query)
    {
        var errors = new List<string>();

        if (query.PageId == Guid.Empty)
        {
            errors.Add("PageId không hợp lệ.");
        }

        if (query.PageNumber <= 0)
        {
            errors.Add("PageNumber phải lớn hơn 0.");
        }

        if (query.PageSize <= 0 || query.PageSize > 100)
        {
            errors.Add("PageSize phải nằm trong khoảng 1-100.");
        }

        if (query.DueFrom.HasValue && query.DueTo.HasValue && query.DueFrom.Value > query.DueTo.Value)
        {
            errors.Add("DueFrom không được lớn hơn DueTo.");
        }

        if (query.AssigneeUserId == Guid.Empty)
        {
            errors.Add("AssigneeUserId không hợp lệ.");
        }

        return errors;
    }
}