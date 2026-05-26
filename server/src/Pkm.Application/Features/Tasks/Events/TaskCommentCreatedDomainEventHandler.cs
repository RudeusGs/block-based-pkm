using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Application.Features.Tasks.Events;

public sealed class TaskCommentCreatedDomainEventHandler : IDomainEventHandler<TaskCommentCreatedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskCommentCreatedDomainEventHandler(
        IUserRepository userRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskCommentRepository taskCommentRepository,
        IActivityLogService activityLogService,
        ITaskRealtimePublisher taskRealtimePublisher,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskCommentRepository = taskCommentRepository;
        _activityLogService = activityLogService;
        _taskRealtimePublisher = taskRealtimePublisher;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(
        TaskCommentCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        if (taskDetail is null)
            return;

        var actorDisplayName = await GetActorDisplayNameAsync(domainEvent.UserId, cancellationToken);
        var comment = await _taskCommentRepository.GetByIdAsync(domainEvent.CommentId, cancellationToken);
        var parentComment = domainEvent.ParentCommentId.HasValue
            ? await _taskCommentRepository.GetByIdIncludingDeletedAsync(domainEvent.ParentCommentId.Value, cancellationToken)
            : null;

        var dto = comment?.ToDto();
        var isReply = domainEvent.ParentCommentId.HasValue;

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                taskDetail.WorkspaceId,
                domainEvent.UserId,
                ActivityAction.Create,
                ActivityEntityType.TaskComment,
                domainEvent.CommentId,
                isReply
                    ? $"{actorDisplayName} đã phản hồi trong task \"{taskDetail.Title}\"."
                    : $"{actorDisplayName} đã bình luận trong task \"{taskDetail.Title}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = taskDetail.Id,
                    taskTitle = taskDetail.Title,
                    pageId = taskDetail.PageId,
                    commentId = domainEvent.CommentId,
                    parentCommentId = domainEvent.ParentCommentId,
                    contentPreview = Preview(domainEvent.Content)
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskCommentCreated",
                WorkspaceId: taskDetail.WorkspaceId,
                PageId: taskDetail.PageId,
                TaskId: taskDetail.Id,
                ActorId: domainEvent.UserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: new
                {
                    comment = dto
                }),
            cancellationToken);

        var recipients = taskDetail.Assignees
            .Select(x => x.UserId)
            .Append(taskDetail.CreatedById)
            .Concat(parentComment is null ? Array.Empty<Guid>() : new[] { parentComment.UserId })
            .Distinct()
            .ToArray();

        await _notificationService.NotifyManyAsync(
            recipients,
            NotificationTemplates.TaskCommentCreated(
                domainEvent.UserId,
                actorDisplayName,
                taskDetail.WorkspaceId,
                taskDetail.Id,
                domainEvent.CommentId,
                taskDetail.Title,
                isReply),
            excludeUserIds: new[] { domainEvent.UserId },
            cancellationToken);
    }

    private async Task<string> GetActorDisplayNameAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actor = await _userRepository.GetByIdAsync(actorUserId, cancellationToken);
        return actor?.UserName ?? "Có người";
    }

    private static string Preview(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= 160 ? normalized : $"{normalized[..160]}...";
    }
}
