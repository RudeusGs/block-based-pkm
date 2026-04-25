namespace Pkm.Application.Features.Tasks.Queries.ListWorkspaceTasks;

public sealed class ListWorkspaceTasksQueryValidator
{
    public IReadOnlyList<string> Validate(ListWorkspaceTasksQuery query)
    {
        var errors = new List<string>();

        if (query.WorkspaceId == Guid.Empty)
        {
            errors.Add("WorkspaceId không hợp lệ.");
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