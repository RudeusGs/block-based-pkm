using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;

public sealed class UpdateTaskCommentHandler : ICommandHandler<UpdateTaskCommentCommand, TaskCommentDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly UpdateTaskCommentCommandValidator _validator;
    private readonly IActivityLogService _activityLogService;

    public UpdateTaskCommentHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        UpdateTaskCommentCommandValidator validator,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _taskCommentRepository = taskCommentRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
        _validator = validator;
        _activityLogService = activityLogService;
    }

    public async Task<Result<TaskCommentDto>> HandleAsync(
        UpdateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.InvalidUpdateRequest(validationErrors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<TaskCommentDto>(TaskErrors.MissingUserContext);

        var comment = await _taskCommentRepository.GetByIdForUpdateAsync(
            request.CommentId,
            cancellationToken);

        if (comment is null)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentNotFound);

        var access = await _taskAccessEvaluator.EvaluateAsync(
            comment.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<TaskCommentDto>(TaskErrors.TaskNotFound);

        if (!access.CanReadTask)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentReadForbidden);

        if (comment.UserId != currentUserId)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentForbidden);

        var task = await _workTaskWriteRepository.GetByIdAsync(
            comment.TaskId,
            cancellationToken);

        if (task is null)
            return Result.Failure<TaskCommentDto>(TaskErrors.TaskNotFound);

        try
        {
            var now = _clock.UtcNow;
            var oldContent = comment.Content;

            comment.UpdateContent(request.Content, currentUserId, now);
            _taskCommentRepository.Update(comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCommentListCacheAsync(comment.TaskId, cancellationToken);

            var dto = comment.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    task.WorkspaceId,
                    currentUserId,
                    ActivityAction.Update,
                    ActivityEntityType.TaskComment,
                    comment.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã cập nhật bình luận trong task \"{task.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        taskId = task.Id,
                        taskTitle = task.Title,
                        pageId = task.PageId,
                        commentId = comment.Id,
                        oldContentPreview = Preview(oldContent),
                        newContentPreview = Preview(request.Content)
                    })),
                cancellationToken);

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskCommentUpdated",
                    WorkspaceId: task.WorkspaceId,
                    PageId: task.PageId,
                    TaskId: task.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        comment = dto
                    }),
                cancellationToken);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.OperationFailed(ex.Message));
        }
    }

    private async Task InvalidateCommentListCacheAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var versionKey = TaskCommentCacheKeys.ListVersion(_cacheKeyFactory, taskId);

        await _cache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }

    private static string Preview(string value)
    {
        var normalized = value.Trim();

        return normalized.Length <= 160
            ? normalized
            : $"{normalized[..160]}...";
    }
}
