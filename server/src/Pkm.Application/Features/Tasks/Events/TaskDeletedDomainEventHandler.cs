using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Application.Features.Tasks.Events;

public sealed class TaskDeletedDomainEventHandler : IDomainEventHandler<TaskDeletedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskDeletedDomainEventHandler(
        IUserRepository userRepository,
        IActivityLogService activityLogService,
        ITaskRealtimePublisher taskRealtimePublisher,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _activityLogService = activityLogService;
        _taskRealtimePublisher = taskRealtimePublisher;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(
        TaskDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actorDisplayName = await GetActorDisplayNameAsync(
            domainEvent.DeletedByUserId,
            cancellationToken);

        var assigneeIds = domainEvent.AssigneeUserIds.ToArray();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                domainEvent.WorkspaceId,
                domainEvent.DeletedByUserId,
                ActivityAction.Delete,
                ActivityEntityType.WorkTask,
                domainEvent.TaskId,
                $"{actorDisplayName} đã xóa task \"{domainEvent.TaskTitle}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = domainEvent.TaskId,
                    title = domainEvent.TaskTitle,
                    pageId = domainEvent.PageId,
                    assigneeUserIds = assigneeIds
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskDeleted",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.PageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.DeletedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: new
                {
                    taskId = domainEvent.TaskId
                }),
            cancellationToken);

        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskDeleted(
                domainEvent.DeletedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.TaskTitle),
            excludeUserIds: new[] { domainEvent.DeletedByUserId },
            cancellationToken);
    }

    private async Task<string> GetActorDisplayNameAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _userRepository.GetByIdAsync(actorUserId, cancellationToken);
        return actor?.UserName ?? "Có người";
    }
}
