using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Tasks.Policies;

public interface ITaskAccessEvaluator
{
    Task<TaskAccessResult> EvaluateAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record TaskAccessResult(
    bool Exists,
    Guid TaskId,
    Guid WorkspaceId,
    WorkspaceRole? Role,
    bool CanReadTask,
    bool CanEditTask,
    bool CanAssignTask,
    bool CanCompleteTask,
    bool CanCommentTask,
    bool CanModerateComments)
{
    public bool CanRead => CanReadTask;
    public bool CanWrite => CanEditTask;
}