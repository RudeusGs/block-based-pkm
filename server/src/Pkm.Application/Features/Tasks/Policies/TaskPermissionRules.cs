using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Tasks.Policies;

public static class TaskPermissionRules
{
    public static bool CanManageTasks(WorkspaceRole? role)
        => role is WorkspaceRole.Owner or WorkspaceRole.Manager;

    public static bool CanAssignTasks(WorkspaceRole? role)
        => CanManageTasks(role);
}