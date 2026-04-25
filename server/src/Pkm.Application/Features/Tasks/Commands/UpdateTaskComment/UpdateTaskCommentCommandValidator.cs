namespace Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;

public sealed class UpdateTaskCommentCommandValidator
{
    public IReadOnlyList<string> Validate(UpdateTaskCommentCommand command)
    {
        var errors = new List<string>();

        if (command.CommentId == Guid.Empty)
            errors.Add("CommentId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(command.Content))
            errors.Add("Nội dung bình luận không được để trống.");

        if (command.Content is { Length: > 2000 })
            errors.Add("Nội dung bình luận không được vượt quá 2000 ký tự.");

        return errors;
    }
}