namespace Pkm.Application.Features.Tasks.Commands.CreateTaskComment;

public sealed class CreateTaskCommentCommandValidator
{
    public IReadOnlyList<string> Validate(CreateTaskCommentCommand command)
    {
        var errors = new List<string>();

        if (command.TaskId == Guid.Empty)
            errors.Add("TaskId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(command.Content))
            errors.Add("Nội dung bình luận không được để trống.");

        if (command.Content is { Length: > 2000 })
            errors.Add("Nội dung bình luận không được vượt quá 2000 ký tự.");

        if (command.ParentId == Guid.Empty)
            errors.Add("ParentId không hợp lệ.");

        return errors;
    }
}