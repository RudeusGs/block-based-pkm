namespace Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;

public sealed record UpdateTaskCommentCommand(
    Guid CommentId,
    string Content);