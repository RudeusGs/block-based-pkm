namespace Pkm.Domain.Notifications;

public enum NotificationType
{
    System = 1,

    WorkspaceInvited = 2,
    WorkspaceRoleChanged = 3,
    WorkspaceMemberRemoved = 4,

    TaskAssigned = 5,
    TaskMentioned = 6,
    TaskCompleted = 7,
    TaskCreated = 8,
    TaskUpdated = 9,
    TaskDeleted = 10,
    TaskUnassigned = 11,
    TaskStatusChanged = 12,

    PageCreated = 13,
    PageUpdated = 14,
    PageDeleted = 15,

    TaskCommentCreated = 16,
    TaskCommentReplied = 17,

    RecommendationGenerated = 18
}