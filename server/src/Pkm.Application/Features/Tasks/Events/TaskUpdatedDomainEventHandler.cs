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

public sealed class TaskUpdatedDomainEventHandler : IDomainEventHandler<TaskUpdatedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;

    public TaskUpdatedDomainEventHandler(
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
        TaskUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var actorDisplayName = await GetActorDisplayNameAsync(
            domainEvent.UpdatedByUserId,
            cancellationToken);

        var taskDetail = await _workTaskReadRepository.GetDetailAsync(
            domainEvent.TaskId,
            cancellationToken);

        var dto = taskDetail?.ToDto();
        var assigneeIds = dto?.Assignees.Select(x => x.UserId).ToArray() ?? Array.Empty<Guid>();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                domainEvent.WorkspaceId,
                domainEvent.UpdatedByUserId,
                domainEvent.OldPageId == domainEvent.NewPageId ? ActivityAction.Update : ActivityAction.Move,
                ActivityEntityType.WorkTask,
                domainEvent.TaskId,
                $"{actorDisplayName} đã cập nhật task \"{domainEvent.NewTitle}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = domainEvent.TaskId,
                    oldTitle = domainEvent.OldTitle,
                    newTitle = domainEvent.NewTitle,
                    oldDescription = domainEvent.OldDescription,
                    newDescription = domainEvent.NewDescription,
                    oldPriority = domainEvent.OldPriority.ToString(),
                    newPriority = domainEvent.NewPriority.ToString(),
                    oldDueDate = domainEvent.OldDueDate,
                    newDueDate = domainEvent.NewDueDate,
                    oldPageId = domainEvent.OldPageId,
                    newPageId = domainEvent.NewPageId
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskUpdated",
                WorkspaceId: domainEvent.WorkspaceId,
                PageId: domainEvent.NewPageId,
                TaskId: domainEvent.TaskId,
                ActorId: domainEvent.UpdatedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: dto),
            cancellationToken);

        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskUpdated(
                domainEvent.UpdatedByUserId,
                actorDisplayName,
                domainEvent.WorkspaceId,
                domainEvent.TaskId,
                domainEvent.NewTitle),
            excludeUserIds: new[] { domainEvent.UpdatedByUserId },
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
