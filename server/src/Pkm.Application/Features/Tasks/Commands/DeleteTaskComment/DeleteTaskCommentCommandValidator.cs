namespace Pkm.Application.Features.Tasks.Commands.DeleteTaskComment;

public sealed class DeleteTaskCommentCommandValidator
{
    public IReadOnlyList<string> Validate(DeleteTaskCommentCommand command)
    {
        if (command.CommentId == Guid.Empty)
            return new[] { "CommentId không hợp lệ." };

        return Array.Empty<string>();
    }
}