namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed class ChangeWorkTaskStatusCommandValidator
{
    public IReadOnlyList<string> Validate(ChangeWorkTaskStatusCommand command)
    {
        if (command.TaskId == Guid.Empty)
        {
            return new[] { "TaskId không hợp lệ." };
        }

        return Array.Empty<string>();
    }
}