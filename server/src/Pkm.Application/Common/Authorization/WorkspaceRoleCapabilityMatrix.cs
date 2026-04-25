using Pkm.Domain.Workspaces;

namespace Pkm.Application.Common.Authorization;

public static class WorkspaceRoleCapabilityMatrix
{
    public static WorkspaceCapabilitySet ForWorkspace(bool isOwner, WorkspaceRole? role)
    {
        if (isOwner || role == WorkspaceRole.Owner)
        {
            return new WorkspaceCapabilitySet(
                CanReadWorkspace: true,
                CanUpdateWorkspace: true,
                CanDeleteWorkspace: true,
                CanManageMembers: true,
                CanManageWorkspaceSettings: true,
                CanCreatePages: true,
                CanCreateTasks: true,
                CanReadAudit: true,
                CanManageAuditRetention: true);
        }

        return role switch
        {
            WorkspaceRole.Manager => new WorkspaceCapabilitySet(
                CanReadWorkspace: true,
                CanUpdateWorkspace: true,
                CanDeleteWorkspace: false,
                CanManageMembers: true,
                CanManageWorkspaceSettings: false,
                CanCreatePages: true,
                CanCreateTasks: true,
                CanReadAudit: true,
                CanManageAuditRetention: false),

            WorkspaceRole.Member => new WorkspaceCapabilitySet(
                CanReadWorkspace: true,
                CanUpdateWorkspace: false,
                CanDeleteWorkspace: false,
                CanManageMembers: false,
                CanManageWorkspaceSettings: false,
                CanCreatePages: true,
                CanCreateTasks: true,
                CanReadAudit: false,
                CanManageAuditRetention: false),

            WorkspaceRole.Viewer => new WorkspaceCapabilitySet(
                CanReadWorkspace: true,
                CanUpdateWorkspace: false,
                CanDeleteWorkspace: false,
                CanManageMembers: false,
                CanManageWorkspaceSettings: false,
                CanCreatePages: false,
                CanCreateTasks: false,
                CanReadAudit: false,
                CanManageAuditRetention: false),

            _ => default
        };
    }

    public static PageCapabilitySet ForPage(bool isOwner, WorkspaceRole? role, bool isArchived)
    {
        if (isArchived)
        {
            return ForArchivedPage(isOwner, role);
        }

        if (isOwner || role == WorkspaceRole.Owner)
        {
            return new PageCapabilitySet(
                CanReadPage: true,
                CanCreateSubPage: true,
                CanEditPageMetadata: true,
                CanArchivePage: true,
                CanManagePage: true,
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true);
        }

        return role switch
        {
            WorkspaceRole.Manager => new PageCapabilitySet(
                CanReadPage: true,
                CanCreateSubPage: true,
                CanEditPageMetadata: true,
                CanArchivePage: true,
                CanManagePage: true,
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true),

            WorkspaceRole.Member => new PageCapabilitySet(
                CanReadPage: true,
                CanCreateSubPage: true,
                CanEditPageMetadata: false,
                CanArchivePage: false,
                CanManagePage: false,
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true),

            WorkspaceRole.Viewer => new PageCapabilitySet(
                CanReadPage: true,
                CanCreateSubPage: false,
                CanEditPageMetadata: false,
                CanArchivePage: false,
                CanManagePage: false,
                CanReadDocument: true,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false),

            _ => default
        };
    }

    public static DocumentCapabilitySet ForDocument(bool isOwner, WorkspaceRole? role, bool isPageArchived)
    {
        if (isPageArchived)
        {
            return ForArchivedDocument(isOwner, role);
        }

        if (isOwner || role == WorkspaceRole.Owner)
        {
            return new DocumentCapabilitySet(
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true,
                CanManagePage: true);
        }

        return role switch
        {
            WorkspaceRole.Manager => new DocumentCapabilitySet(
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true,
                CanManagePage: true),

            WorkspaceRole.Member => new DocumentCapabilitySet(
                CanReadDocument: true,
                CanEditDocument: true,
                CanReorderBlocks: true,
                CanDeleteBlocks: true,
                CanAcquireLease: true,
                CanManagePage: false),

            WorkspaceRole.Viewer => new DocumentCapabilitySet(
                CanReadDocument: true,
                CanEditDocument: false,
                CanReorderBlocks: false,
                CanDeleteBlocks: false,
                CanAcquireLease: false,
                CanManagePage: false),

            _ => default
        };
    }

    public static TaskCapabilitySet ForTask(bool isOwner, WorkspaceRole? role)
    {
        if (isOwner || role == WorkspaceRole.Owner)
        {
            return new TaskCapabilitySet(
                CanReadTask: true,
                CanEditTask: true,
                CanAssignTask: true,
                CanCompleteTask: true,
                CanCommentTask: true,
                CanModerateComments: true);
        }

        return role switch
        {
            WorkspaceRole.Manager => new TaskCapabilitySet(
                CanReadTask: true,
                CanEditTask: true,
                CanAssignTask: true,
                CanCompleteTask: true,
                CanCommentTask: true,
                CanModerateComments: true),

            // Members may read and comment, but they cannot manage, assign, delete,
            // or complete arbitrary tasks. An assigned member can still change the
            // status of their own assigned task in ChangeWorkTaskStatusHandler.
            WorkspaceRole.Member => new TaskCapabilitySet(
                CanReadTask: true,
                CanEditTask: false,
                CanAssignTask: false,
                CanCompleteTask: false,
                CanCommentTask: true,
                CanModerateComments: false),

            WorkspaceRole.Viewer => new TaskCapabilitySet(
                CanReadTask: true,
                CanEditTask: false,
                CanAssignTask: false,
                CanCompleteTask: false,
                CanCommentTask: false,
                CanModerateComments: false),

            _ => default
        };
    }

    private static PageCapabilitySet ForArchivedPage(bool isOwner, WorkspaceRole? role)
    {
        var canRead = isOwner || role is WorkspaceRole.Owner or WorkspaceRole.Manager or WorkspaceRole.Member or WorkspaceRole.Viewer;

        return new PageCapabilitySet(
            CanReadPage: canRead,
            CanCreateSubPage: false,
            CanEditPageMetadata: false,
            CanArchivePage: false,
            CanManagePage: isOwner || role is WorkspaceRole.Owner or WorkspaceRole.Manager,
            CanReadDocument: canRead,
            CanEditDocument: false,
            CanReorderBlocks: false,
            CanDeleteBlocks: false,
            CanAcquireLease: false);
    }

    private static DocumentCapabilitySet ForArchivedDocument(bool isOwner, WorkspaceRole? role)
    {
        var canRead = isOwner || role is WorkspaceRole.Owner or WorkspaceRole.Manager or WorkspaceRole.Member or WorkspaceRole.Viewer;

        return new DocumentCapabilitySet(
            CanReadDocument: canRead,
            CanEditDocument: false,
            CanReorderBlocks: false,
            CanDeleteBlocks: false,
            CanAcquireLease: false,
            CanManagePage: isOwner || role is WorkspaceRole.Owner or WorkspaceRole.Manager);
    }
}

public readonly record struct WorkspaceCapabilitySet(
    bool CanReadWorkspace,
    bool CanUpdateWorkspace,
    bool CanDeleteWorkspace,
    bool CanManageMembers,
    bool CanManageWorkspaceSettings,
    bool CanCreatePages,
    bool CanCreateTasks,
    bool CanReadAudit,
    bool CanManageAuditRetention);

public readonly record struct PageCapabilitySet(
    bool CanReadPage,
    bool CanCreateSubPage,
    bool CanEditPageMetadata,
    bool CanArchivePage,
    bool CanManagePage,
    bool CanReadDocument,
    bool CanEditDocument,
    bool CanReorderBlocks,
    bool CanDeleteBlocks,
    bool CanAcquireLease);

public readonly record struct DocumentCapabilitySet(
    bool CanReadDocument,
    bool CanEditDocument,
    bool CanReorderBlocks,
    bool CanDeleteBlocks,
    bool CanAcquireLease,
    bool CanManagePage);

public readonly record struct TaskCapabilitySet(
    bool CanReadTask,
    bool CanEditTask,
    bool CanAssignTask,
    bool CanCompleteTask,
    bool CanCommentTask,
    bool CanModerateComments);