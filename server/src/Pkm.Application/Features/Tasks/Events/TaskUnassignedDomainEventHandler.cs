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

public sealed class TaskUnassignedDomainEventHandler : IDomainEventHandler<TaskUnassignedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskUnassignedDomainEventHandler(
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
        TaskUnassignedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actor = await _userRepository.GetByIdAsync(
            domainEvent.UnassignedByUserId,
            cancellationToken);

        var actorDisplayName = actor?.UserName ?? "Có người";

        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        var dto = taskDetail?.ToDto();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                domainEvent.WorkspaceId,
                domainEvent.UnassignedByUserId,
                ActivityAction.Unassign,
                ActivityEntityType.TaskAssignee,
                domainEvent.TaskId,
                $"{actorDisplayName} đã gỡ phân công task \"{domainEvent.TaskTitle}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = domainEvent.TaskId,
                    title = domainEvent.TaskTitle,
                    pageId = domainEvent.PageId,
                    assigneeUserId = domainEvent.AssigneeUserId
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskUnassigned",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.PageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.UnassignedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: new
                {
                    task = dto,
                    assigneeUserId = domainEvent.AssigneeUserId
                }),
            cancellationToken);

        await _notificationService.NotifyAsync(
            domainEvent.AssigneeUserId,
            NotificationTemplates.TaskUnassigned(
                domainEvent.UnassignedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.TaskTitle),
            cancellationToken);
    }
}
