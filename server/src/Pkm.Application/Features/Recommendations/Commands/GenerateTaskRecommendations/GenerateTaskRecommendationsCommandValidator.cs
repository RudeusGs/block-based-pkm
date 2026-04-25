namespace Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;

public sealed class GenerateTaskRecommendationsCommandValidator
{
    public IReadOnlyList<string> Validate(GenerateTaskRecommendationsCommand command)
    {
        var errors = new List<string>();

        if (command.WorkspaceId == Guid.Empty)
            errors.Add("WorkspaceId không hợp lệ.");

        if (command.PageId == Guid.Empty)
            errors.Add("PageId không hợp lệ.");

        return errors;
    }
}