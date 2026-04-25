namespace Pkm.Application.Features.Recommendations.Queries.ListTaskRecommendations;

public sealed class ListTaskRecommendationsQueryValidator
{
    public IReadOnlyList<string> Validate(ListTaskRecommendationsQuery query)
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