using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(
    Guid TaskId,
    Guid AssigneeUserId) : ICommand<WorkTaskDto>;
