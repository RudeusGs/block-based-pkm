namespace Pkm.Application.Features.Tasks.Queries.ListTaskComments;

public sealed class ListTaskCommentsQueryValidator
{
    public IReadOnlyList<string> Validate(ListTaskCommentsQuery query)
    {
        var errors = new List<string>();

        if (query.TaskId == Guid.Empty)
            errors.Add("TaskId không hợp lệ.");

        if (query.PageNumber <= 0)
            errors.Add("PageNumber phải lớn hơn 0.");

        if (query.PageSize <= 0 || query.PageSize > 100)
            errors.Add("PageSize phải nằm trong khoảng 1-100.");

        return errors;
    }
}