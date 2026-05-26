using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Messaging;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Application.Features.Tasks.Events;

public sealed class TaskCommentDeletedDomainEventHandler : IDomainEventHandler<TaskCommentDeletedDomainEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;

    public TaskCommentDeletedDomainEventHandler(
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
        TaskCommentDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var taskDetail = await _workTaskReadRepository.GetDetailAsync(domainEvent.TaskId, cancellationToken);
        if (taskDetail is null)
            return;

        var actorDisplayName = await GetActorDisplayNameAsync(domainEvent.DeletedByUserId, cancellationToken);
        var comment = await _taskCommentRepository.GetByIdIncludingDeletedAsync(domainEvent.CommentId, cancellationToken);
        var dto = comment?.ToDto();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                taskDetail.WorkspaceId,
                domainEvent.DeletedByUserId,
                ActivityAction.Delete,
                ActivityEntityType.TaskComment,
                domainEvent.CommentId,
                $"{actorDisplayName} đã xóa bình luận trong task \"{taskDetail.Title}\".",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = taskDetail.Id,
                    taskTitle = taskDetail.Title,
                    pageId = taskDetail.PageId,
                    commentId = domainEvent.CommentId,
                    commentOwnerUserId = domainEvent.CommentOwnerUserId,
                    deletedByModeration = domainEvent.DeletedByModeration,
                    contentPreview = Preview(domainEvent.OldContent)
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskCommentDeleted",
                WorkspaceId: taskDetail.WorkspaceId,
                PageId: taskDetail.PageId,
                TaskId: taskDetail.Id,
                ActorId: domainEvent.DeletedByUserId,
                OccurredAtUtc: domainEvent.OccurredAtUtc,
                Payload: new
                {
                    comment = dto,
                    deletedByModeration = domainEvent.DeletedByModeration
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
