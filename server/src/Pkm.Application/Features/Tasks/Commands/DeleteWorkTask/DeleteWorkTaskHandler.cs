using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Policies;

namespace Pkm.Application.Features.Tasks.Commands.DeleteWorkTask;

public sealed class DeleteWorkTaskHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;
    private readonly DeleteWorkTaskCommandValidator _validator;
    private readonly INotificationService _notificationService;
    
    public DeleteWorkTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock,
        DeleteWorkTaskCommandValidator validator,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
        _validator = validator;
        _notificationService = notificationService;
    }

    public async Task<Result> HandleAsync(
        DeleteWorkTaskCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure(TaskErrors.InvalidDeleteRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(TaskErrors.MissingUserContext);
        }

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure(TaskErrors.TaskNotFound);
        }

        if (!TaskPermissionRules.CanManageTasks(access.Role))
        {
            return Result.Failure(TaskErrors.TaskManageForbidden);
        }

        var task = await _workTaskRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure(TaskErrors.TaskNotFound);
        }

        var now = _clock.UtcNow;
        var detailBeforeDelete = await _workTaskRepository.GetDetailAsync(
            task.Id,
            cancellationToken);

        var assigneeIds = detailBeforeDelete?.Assignees
            .Select(x => x.UserId)
            .ToArray() ?? Array.Empty<Guid>();

        task.Delete(currentUserId, now);
        _workTaskRepository.Update(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskDeleted",
                WorkspaceId: task.WorkspaceId,
                PageId: task.PageId,
                TaskId: task.Id,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Payload: new
                {
                    taskId = task.Id
                }),
            cancellationToken);
        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskDeleted(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                task.WorkspaceId,
                task.Id,
                task.Title),
            excludeUserIds: new[] { currentUserId },
            cancellationToken);
        return Result.Success();
    }
}
