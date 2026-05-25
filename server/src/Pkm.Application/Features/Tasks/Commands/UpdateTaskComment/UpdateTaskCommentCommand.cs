using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;

public sealed record UpdateTaskCommentCommand(
    Guid CommentId,
    string Content) : ICommand<TaskCommentDto>;
