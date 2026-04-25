namespace Pkm.Application.Features.Tasks.Commands.AssignTask;

public sealed class AssignTaskCommandValidator
{
    public IReadOnlyList<string> Validate(AssignTaskCommand command)
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