using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.CreateTaskComment;

public sealed record CreateTaskCommentCommand(
    Guid TaskId,
    string Content,
    Guid? ParentId = null) : ICommand<TaskCommentDto>;
