using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.RestoreTaskComment;

public sealed record RestoreTaskCommentCommand(Guid CommentId) : ICommand<TaskCommentDto>;
