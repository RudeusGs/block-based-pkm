namespace Pkm.Application.Features.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(
    Guid TaskId,
    Guid AssigneeUserId);