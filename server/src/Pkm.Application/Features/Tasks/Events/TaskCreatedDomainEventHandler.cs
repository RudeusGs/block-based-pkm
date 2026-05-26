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

public sealed class TaskCreatedDomainEventHandler : IDomainEventHandler<TaskCreatedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskCreatedDomainEventHandler(
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
        TaskCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actorDisplayName = await GetActorDisplayNameAsync(
            domainEvent.CreatedByUserId,
            cancellationToken);

        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        var dto = taskDetail?.ToDto();
        var assigneeIds = domainEvent.AssigneeUserIds.ToArray();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                domainEvent.WorkspaceId,
                domainEvent.CreatedByUserId,
                ActivityAction.Create,
                ActivityEntityType.WorkTask,
                domainEvent.TaskId,
                $"{actorDisplayName} đã tạo task \"{domainEvent.TaskTitle}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = domainEvent.TaskId,
                    title = domainEvent.TaskTitle,
                    pageId = domainEvent.PageId,
                    priority = domainEvent.Priority.ToString(),
                    dueDate = domainEvent.DueDate,
                    assigneeUserIds = assigneeIds
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskCreated",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.PageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.CreatedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: dto),
            cancellationToken);

        await _notificationService.NotifyWorkspaceAsync(
            domainEvent.WorkspaceId,
            NotificationTemplates.TaskCreated(
                domainEvent.CreatedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.TaskTitle),
            excludeUserIds: new[] { domainEvent.CreatedByUserId }.Concat(assigneeIds),
            cancellationToken);

        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskAssigned(
                domainEvent.CreatedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.TaskTitle),
            excludeUserIds: new[] { domainEvent.CreatedByUserId },
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
