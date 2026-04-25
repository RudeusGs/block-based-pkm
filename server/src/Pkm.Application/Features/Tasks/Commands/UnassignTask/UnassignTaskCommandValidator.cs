namespace Pkm.Application.Features.Tasks.Commands.UnassignTask;

public sealed class UnassignTaskCommandValidator
{
    public IReadOnlyList<string> Validate(UnassignTaskCommand command)
    {
        var errors = new List<string>();

        if (command.TaskId == Guid.Empty)
        {
            errors.Add("TaskId không hợp lệ.");
        }

        if (command.AssigneeUserId == Guid.Empty)
        {
            errors.Add("AssigneeUserId không hợp lệ.");
        }

        return errors;
    }
}
