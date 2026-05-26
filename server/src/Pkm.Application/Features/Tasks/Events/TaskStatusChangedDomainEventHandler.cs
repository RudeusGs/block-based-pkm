using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Application.Features.Tasks.Events;

public sealed class TaskStatusChangedDomainEventHandler : IDomainEventHandler<TaskStatusChangedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskStatusChangedDomainEventHandler(
        IUserRepository userRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        IActivityLogService activityLogService,
        ITaskRealtimePublisher taskRealtimePublisher,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _activityLogService = activityLogService;
        _taskRealtimePublisher = taskRealtimePublisher;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(
        TaskStatusChangedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actorDisplayName = await GetActorDisplayNameAsync(
            domainEvent.ChangedByUserId,
            cancellationToken);

        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        var dto = taskDetail?.ToDto();
        var assigneeIds = dto?.Assignees.Select(x => x.UserId).ToArray() ?? Array.Empty<Guid>();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                domainEvent.WorkspaceId,
                domainEvent.ChangedByUserId,
                ResolveStatusAction(domainEvent.OldStatus, domainEvent.NewStatus),
                ActivityEntityType.WorkTask,
                domainEvent.TaskId,
                $"{actorDisplayName} đã chuyển task \"{domainEvent.TaskTitle}\" từ {domainEvent.OldStatus} sang {domainEvent.NewStatus}.",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = domainEvent.TaskId,
                    title = domainEvent.TaskTitle,
                    pageId = domainEvent.PageId,
                    oldStatus = domainEvent.OldStatus.ToString(),
                    newStatus = domainEvent.NewStatus.ToString()
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskStatusChanged",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.PageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.ChangedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: new
                {
                    task = dto,
                    status = domainEvent.NewStatus.ToString()
                }),
            cancellationToken);

        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskStatusChanged(
                domainEvent.ChangedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.TaskTitle,
                domainEvent.NewStatus),
            excludeUserIds: new[] { domainEvent.ChangedByUserId },
            cancellationToken);
    }

    private async Task<string> GetActorDisplayNameAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _userRepository.GetByIdAsync(actorUserId, cancellationToken);
        return actor?.UserName ?? "Có người";
    }

    private static ActivityAction ResolveStatusAction(
        StatusWorkTask oldStatus,
        StatusWorkTask newStatus)
    {
        if (newStatus == StatusWorkTask.Done)
            return ActivityAction.Complete;

        if (oldStatus == StatusWorkTask.Done)
            return ActivityAction.Reopen;

        return ActivityAction.Update;
    }
}
