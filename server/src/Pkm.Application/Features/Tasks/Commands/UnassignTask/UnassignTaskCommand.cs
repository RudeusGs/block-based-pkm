using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Commands.UnassignTask;

public sealed record UnassignTaskCommand(
    Guid TaskId,
    Guid AssigneeUserId) : ICommand<WorkTaskDto>;
