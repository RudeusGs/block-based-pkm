using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.CreateTaskComment;

public sealed class CreateTaskCommentHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly INotificationService _notificationService;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;
    private readonly CreateTaskCommentCommandValidator _validator;

    public CreateTaskCommentHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        INotificationService notificationService,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock,
        CreateTaskCommentCommandValidator validator)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskCommentRepository = taskCommentRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _notificationService = notificationService;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<TaskCommentDto>> HandleAsync(
        CreateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.InvalidCreateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<TaskCommentDto>(
                TaskErrors.MissingUserContext);
        }

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<TaskCommentDto>(
                TaskErrors.TaskNotFound);
        }

        if (!access.CanCommentTask)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.CommentCreateForbidden);
        }

        var task = await _workTaskRepository.GetByIdAsync(
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return Result.Failure<TaskCommentDto>(
                TaskErrors.TaskNotFound);
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _taskCommentRepository.ExistsInTaskAsync(
                request.ParentId.Value,
                request.TaskId,
                includeDeleted: false,
                cancellationToken);

            if (!parentExists)
            {
                return Result.Failure<TaskCommentDto>(
                    TaskCommentErrors.ParentCommentNotFound);
            }
        }

        try
        {
            var now = _clock.UtcNow;

            var comment = request.ParentId.HasValue
                ? TaskComment.CreateReply(
                    request.TaskId,
                    currentUserId,
                    request.Content,
                    request.ParentId.Value,
                    now)
                : TaskComment.Create(
                    request.TaskId,
                    currentUserId,
                    request.Content,
                    now);

            _taskCommentRepository.Add(comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCommentListCacheAsync(task.Id, cancellationToken);

            var dto = comment.ToDto();

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskCommentCreated",
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

            var taskDetail = await _workTaskRepository.GetDetailAsync(
                task.Id,
                cancellationToken);

            var recipients = taskDetail is null
                ? new[] { task.CreatedById }
                : taskDetail.Assignees
                    .Select(x => x.UserId)
                    .Append(taskDetail.CreatedById)
                    .Distinct()
                    .ToArray();

            await _notificationService.NotifyManyAsync(
                recipients,
                NotificationTemplates.TaskCommentCreated(
                    currentUserId,
                    _currentUser.UserName ?? "Có người",
                    task.WorkspaceId,
                    task.Id,
                    comment.Id,
                    task.Title,
                    request.ParentId.HasValue),
                excludeUserIds: new[] { currentUserId },
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
        var versionKey = TaskCommentCacheKeys.ListVersion(
            _redisKeyFactory,
            taskId);

        await _redisCache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }
}