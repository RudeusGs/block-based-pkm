namespace Pkm.Application.Features.Tasks.Commands.DeleteWorkTask;

public sealed class DeleteWorkTaskCommandValidator
{
    public IReadOnlyList<string> Validate(DeleteWorkTaskCommand command)
    {
        if (command.TaskId == Guid.Empty)
        {
            return new[] { "TaskId không hợp lệ." };
        }

        return Array.Empty<string>();
    }
}