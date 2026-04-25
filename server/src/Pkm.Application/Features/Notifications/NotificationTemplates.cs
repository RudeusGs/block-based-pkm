using Pkm.Application.Features.Notifications.Services;
using Pkm.Domain.Notifications;
using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Notifications;

public static class NotificationTemplates
{
    public static NotificationDispatchRequest WorkspaceInvited(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        WorkspaceRole role)
        => new(
            Type: NotificationType.WorkspaceInvited,
            Title: "Bạn đã được thêm vào workspace",
            Message: $"{actorDisplayName} đã thêm bạn vào workspace với vai trò {role}.",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: workspaceId,
            ReferenceType: NotificationReferenceTypes.Workspace);

    public static NotificationDispatchRequest WorkspaceRoleChanged(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        WorkspaceRole role)
        => new(
            Type: NotificationType.WorkspaceRoleChanged,
            Title: "Vai trò của bạn đã được thay đổi",
            Message: $"{actorDisplayName} đã thay đổi vai trò của bạn trong workspace thành {role}.",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: workspaceId,
            ReferenceType: NotificationReferenceTypes.Workspace);

    public static NotificationDispatchRequest PageCreated(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid pageId,
        string pageTitle)
        => new(
            Type: NotificationType.PageCreated,
            Title: "Page mới trong workspace",
            Message: $"{actorDisplayName} đã tạo page \"{pageTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: pageId,
            ReferenceType: NotificationReferenceTypes.Page);

    public static NotificationDispatchRequest PageUpdated(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid pageId,
        string pageTitle)
        => new(
            Type: NotificationType.PageUpdated,
            Title: "Page đã được cập nhật",
            Message: $"{actorDisplayName} đã cập nhật page \"{pageTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: pageId,
            ReferenceType: NotificationReferenceTypes.Page);

    public static NotificationDispatchRequest PageDeleted(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid pageId,
        string pageTitle)
        => new(
            Type: NotificationType.PageDeleted,
            Title: "Page đã bị xóa",
            Message: $"{actorDisplayName} đã xóa page \"{pageTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: pageId,
            ReferenceType: NotificationReferenceTypes.Page);

    public static NotificationDispatchRequest TaskCreated(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle)
        => new(
            Type: NotificationType.TaskCreated,
            Title: "Task mới trong workspace",
            Message: $"{actorDisplayName} đã tạo task \"{taskTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskAssigned(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle)
        => new(
            Type: NotificationType.TaskAssigned,
            Title: "Bạn được phân công công việc",
            Message: $"{actorDisplayName} đã phân công công việc \"{taskTitle}\" cho bạn.",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskUnassigned(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle)
        => new(
            Type: NotificationType.TaskUnassigned,
            Title: "Bạn đã được gỡ khỏi công việc",
            Message: $"{actorDisplayName} đã gỡ bạn khỏi công việc \"{taskTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskUpdated(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle)
        => new(
            Type: NotificationType.TaskUpdated,
            Title: "Task đã được cập nhật",
            Message: $"{actorDisplayName} đã cập nhật task \"{taskTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskDeleted(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle)
        => new(
            Type: NotificationType.TaskDeleted,
            Title: "Task đã bị xóa",
            Message: $"{actorDisplayName} đã xóa task \"{taskTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskStatusChanged(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        string taskTitle,
        StatusWorkTask status)
        => new(
            Type: status == StatusWorkTask.Done
                ? NotificationType.TaskCompleted
                : NotificationType.TaskStatusChanged,
            Title: status == StatusWorkTask.Done
                ? "Task đã hoàn thành"
                : "Trạng thái task đã thay đổi",
            Message: status == StatusWorkTask.Done
                ? $"{actorDisplayName} đã hoàn thành task \"{taskTitle}\"."
                : $"{actorDisplayName} đã chuyển task \"{taskTitle}\" sang trạng thái {status}.",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: taskId,
            ReferenceType: NotificationReferenceTypes.Task);

    public static NotificationDispatchRequest TaskCommentCreated(
        Guid actorUserId,
        string actorDisplayName,
        Guid workspaceId,
        Guid taskId,
        Guid commentId,
        string taskTitle,
        bool isReply)
        => new(
            Type: isReply
                ? NotificationType.TaskCommentReplied
                : NotificationType.TaskCommentCreated,
            Title: isReply
                ? "Có phản hồi mới trong task"
                : "Có bình luận mới trong task",
            Message: isReply
                ? $"{actorDisplayName} đã phản hồi trong task \"{taskTitle}\"."
                : $"{actorDisplayName} đã bình luận trong task \"{taskTitle}\".",
            ActorUserId: actorUserId,
            WorkspaceId: workspaceId,
            ReferenceId: commentId,
            ReferenceType: NotificationReferenceTypes.TaskComment);

    public static NotificationDispatchRequest WorkspaceMemberRemoved(
    Guid actorUserId,
    string actorDisplayName,
    Guid workspaceId)
    => new(
        Type: NotificationType.WorkspaceMemberRemoved,
        Title: "Bạn đã bị xóa khỏi workspace",
        Message: $"{actorDisplayName} đã xóa bạn khỏi workspace.",
        ActorUserId: actorUserId,
        WorkspaceId: workspaceId,
        ReferenceId: workspaceId,
        ReferenceType: NotificationReferenceTypes.Workspace);
}