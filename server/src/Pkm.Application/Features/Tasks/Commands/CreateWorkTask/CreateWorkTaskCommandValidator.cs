namespace Pkm.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskCommandValidator
{
    public IReadOnlyList<string> Validate(CreateWorkTaskCommand command)
    {
        var errors = new List<string>();

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

        if (command.AssigneeUserIds is not null && command.AssigneeUserIds.Any(x => x == Guid.Empty))
        {
            errors.Add("Danh sách người được giao chứa UserId không hợp lệ.");
        }

        return errors;
    }
}