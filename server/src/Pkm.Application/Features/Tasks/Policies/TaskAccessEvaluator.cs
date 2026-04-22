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
            return new TaskAccessResult(
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

        var access = await _workTaskRepository.GetAccessContextAsync(
            taskId,
            userId,
            cancellationToken);

        if (access is null)
        {
            return new TaskAccessResult(
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

        var isOwner = access.CreatedById == userId || access.Role == WorkspaceRole.Owner;
        var capabilities = WorkspaceRoleCapabilityMatrix.ForTask(isOwner, access.Role);

        return new TaskAccessResult(
            Exists: true,
            TaskId: access.TaskId,
            WorkspaceId: access.WorkspaceId,
            Role: isOwner ? WorkspaceRole.Owner : access.Role,
            CanReadTask: capabilities.CanReadTask,
            CanEditTask: capabilities.CanEditTask,
            CanAssignTask: capabilities.CanAssignTask,
            CanCompleteTask: capabilities.CanCompleteTask,
            CanCommentTask: capabilities.CanCommentTask,
            CanModerateComments: capabilities.CanModerateComments);
    }
}