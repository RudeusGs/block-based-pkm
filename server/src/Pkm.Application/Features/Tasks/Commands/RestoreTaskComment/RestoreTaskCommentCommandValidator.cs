namespace Pkm.Application.Features.Tasks.Commands.RestoreTaskComment;

public sealed class RestoreTaskCommentCommandValidator
{
    public IReadOnlyList<string> Validate(RestoreTaskCommentCommand command)
    {
        if (command.CommentId == Guid.Empty)
            return new[] { "CommentId không hợp lệ." };

        return Array.Empty<string>();
    }
}