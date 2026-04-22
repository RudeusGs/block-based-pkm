namespace Pkm.Application.Features.Tasks.Commands.UnassignTask;

public sealed record UnassignTaskCommand(
    Guid TaskId,
    Guid AssigneeUserId);