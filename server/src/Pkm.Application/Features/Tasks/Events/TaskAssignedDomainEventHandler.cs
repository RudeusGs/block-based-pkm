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

public sealed class TaskAssignedDomainEventHandler : IDomainEventHandler<TaskAssignedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskAssignedDomainEventHandler(
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
        TaskAssignedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actor = await _userRepository.GetByIdAsync(
            domainEvent.AssignedByUserId,
            cancellationToken);

        var actorDisplayName = actor?.UserName ?? "Có người";

        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        var dto = taskDetail?.ToDto();

        if (domainEvent.AssigneeUserId != domainEvent.AssignedByUserId)
        {
            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    domainEvent.WorkspaceId,
                    domainEvent.AssignedByUserId,
                    ActivityAction.Assign,
                    ActivityEntityType.TaskAssignee,
                    domainEvent.TaskId,
                    $"{actorDisplayName} đã phân công task \"{domainEvent.TaskTitle}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        taskId = domainEvent.TaskId,
                        title = domainEvent.TaskTitle,
                        pageId = domainEvent.PageId,
                        assigneeUserId = domainEvent.AssigneeUserId
                    })),
                cancellationToken);
        }

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskAssigned",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.PageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.AssignedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: dto),
            cancellationToken);

        if (domainEvent.AssigneeUserId != domainEvent.AssignedByUserId)
        {
            await _notificationService.NotifyAsync(
                domainEvent.AssigneeUserId,
                NotificationTemplates.TaskAssigned(
                    domainEvent.AssignedByUserId,
                    actorDisplayName,
                    domainEvent.WorkspaceId,
                    domainEvent.TaskId,
                    domainEvent.TaskTitle),
                cancellationToken);
        }
    }
}
