namespace Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;

public sealed class UpdateWorkTaskCommandValidator
{
    public IReadOnlyList<string> Validate(UpdateWorkTaskCommand command)
    {
        var errors = new List<string>();

        if (command.TaskId == Guid.Empty)
        {
            errors.Add("TaskId không hợp lệ.");
        }

        if (command.PageId == Guid.Empty)
        {
            errors.Add("PageId không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors.Add("Tiêu đề task không được để trống.");
        }

        if (command.Title is { Length: > 200 })
        {
            errors.Add("Tiêu đề task không được vượt quá 200 ký tự.");
        }

        if (command.Description is { Length: > 4000 })
        {
            errors.Add("Mô tả task không được vượt quá 4000 ký tự.");
        }

        return errors;
    }
}
