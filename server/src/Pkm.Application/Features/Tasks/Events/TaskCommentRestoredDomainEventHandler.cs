using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Application.Features.Tasks.Events;

public sealed class TaskCommentRestoredDomainEventHandler : IDomainEventHandler<TaskCommentRestoredDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;

    public TaskCommentRestoredDomainEventHandler(
        IUserRepository userRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskCommentRepository taskCommentRepository,
        IActivityLogService activityLogService,
        ITaskRealtimePublisher taskRealtimePublisher)
    {
        _userRepository = userRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskCommentRepository = taskCommentRepository;
        _activityLogService = activityLogService;
        _taskRealtimePublisher = taskRealtimePublisher;
    }

    public async Task HandleAsync(
        TaskCommentRestoredDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var taskDetail = await _workTaskReadRepository.GetDetailAsync(domainEvent.TaskId, cancellationToken);
        if (taskDetail is null)
            return;

        var actorDisplayName = await GetActorDisplayNameAsync(domainEvent.UserId, cancellationToken);
        var comment = await _taskCommentRepository.GetByIdAsync(domainEvent.CommentId, cancellationToken);
        var dto = comment?.ToDto();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                taskDetail.WorkspaceId,
                domainEvent.UserId,
                ActivityAction.Restore,
                ActivityEntityType.TaskComment,
                domainEvent.CommentId,
                $"{actorDisplayName} đã khôi phục bình luận trong task \"{taskDetail.Title}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = taskDetail.Id,
                    taskTitle = taskDetail.Title,
                    pageId = taskDetail.PageId,
                    commentId = domainEvent.CommentId,
                    contentPreview = Preview(domainEvent.Content)
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskCommentRestored",
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
