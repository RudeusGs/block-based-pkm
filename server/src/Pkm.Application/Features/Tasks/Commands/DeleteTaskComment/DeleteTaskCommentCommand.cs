using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.DeleteTaskComment;

public sealed record DeleteTaskCommentCommand(Guid CommentId) : ICommand<TaskCommentDto>;
