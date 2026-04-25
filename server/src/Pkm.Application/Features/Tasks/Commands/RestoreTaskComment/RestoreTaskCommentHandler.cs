using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Common;

namespace Pkm.Application.Features.Tasks.Commands.RestoreTaskComment;

public sealed class RestoreTaskCommentHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;
    private readonly RestoreTaskCommentCommandValidator _validator;

    public RestoreTaskCommentHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock,
        RestoreTaskCommentCommandValidator validator)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskCommentRepository = taskCommentRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<TaskCommentDto>> HandleAsync(
        RestoreTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.InvalidRestoreRequest(validationErrors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<TaskCommentDto>(TaskErrors.MissingUserContext);

        var comment = await _taskCommentRepository.GetByIdForUpdateIncludingDeletedAsync(
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

        if (!access.CanCommentTask)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentCreateForbidden);

        if (comment.UserId != currentUserId)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentForbidden);

        var task = await _workTaskRepository.GetByIdAsync(
            comment.TaskId,
            cancellationToken);

        if (task is null)
            return Result.Failure<TaskCommentDto>(TaskErrors.TaskNotFound);

        try
        {
            var now = _clock.UtcNow;

            comment.RestoreByOwner(currentUserId, now);
            _taskCommentRepository.Update(comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCommentListCacheAsync(comment.TaskId, cancellationToken);

            var dto = comment.ToDto();

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskCommentRestored",
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
        var versionKey = TaskCommentCacheKeys.ListVersion(_redisKeyFactory, taskId);

        await _redisCache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }
}