using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Authorization;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Tasks.Policies;

public sealed class TaskAccessEvaluator : ITaskAccessEvaluator
{
    private readonly IWorkTaskRepository _workTaskRepository;

    public TaskAccessEvaluator(IWorkTaskRepository workTaskRepository)
    {
        _workTaskRepository = workTaskRepository;
    }

    public async Task<TaskAccessResult> EvaluateAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty || userId == Guid.Empty)
        {
            return Denied();
        }

        var access = await _workTaskRepository.GetAccessContextAsync(
            taskId,
            userId,
            cancellationToken);

        if (access is null)
        {
            return Denied();
        }

        // Task permission must come from workspace role, not from task creator ownership.
        // A normal member who created a task is still not allowed to assign/delete/manage tasks
        // unless they are promoted to workspace Owner or Manager.
        var isWorkspaceOwner = access.Role == WorkspaceRole.Owner;
        var capabilities = WorkspaceRoleCapabilityMatrix.ForTask(isWorkspaceOwner, access.Role);

        return new TaskAccessResult(
            Exists: true,
            TaskId: access.TaskId,
            WorkspaceId: access.WorkspaceId,
            Role: access.Role,
            CanReadTask: capabilities.CanReadTask,
            CanEditTask: capabilities.CanEditTask,
            CanAssignTask: capabilities.CanAssignTask,
            CanCompleteTask: capabilities.CanCompleteTask,
            CanCommentTask: capabilities.CanCommentTask,
            CanModerateComments: capabilities.CanModerateComments);
    }

    private static TaskAccessResult Denied()
        => new(
            Exists: false,
            TaskId: Guid.Empty,
            WorkspaceId: Guid.Empty,
            Role: null,
            CanReadTask: false,
            CanEditTask: false,
            CanAssignTask: false,
            CanCompleteTask: false,
            CanCommentTask: false,
            CanModerateComments: false);
}