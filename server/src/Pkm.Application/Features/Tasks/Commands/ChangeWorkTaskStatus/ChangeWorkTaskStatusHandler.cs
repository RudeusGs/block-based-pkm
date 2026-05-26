using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed class ChangeWorkTaskStatusHandler : ICommandHandler<ChangeWorkTaskStatusCommand, WorkTaskDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;
    private readonly ChangeWorkTaskStatusCommandValidator _validator;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;
    public ChangeWorkTaskStatusHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock,
        ChangeWorkTaskStatusCommandValidator validator,
        INotificationService notificationService,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
        _validator = validator;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        ChangeWorkTaskStatusCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidStatusChangeRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);
        }

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);
        }

        if (!access.CanReadTask)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);
        }

        var canManageTask = TaskPermissionRules.CanManageTasks(access.Role);
        var isAssignee = await _taskAssigneeRepository.ExistsAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!canManageTask && !isAssignee)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskStatusForbidden);
        }

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);
        }

        if (!task.CanChangeStatusTo(request.Status))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskStatusLocked);
        }

        var now = _clock.UtcNow;
        var oldStatus = task.Status;

        task.ChangeStatus(request.Status, currentUserId, now);
        _workTaskWriteRepository.Update(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskReadRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                task.WorkspaceId,
                currentUserId,
                ResolveStatusAction(oldStatus, request.Status),
                ActivityEntityType.WorkTask,
                task.Id,
                $"{_currentUser.UserName ?? "Có người"} đã chuyển task \"{task.Title}\" từ {oldStatus} sang {request.Status}.",
                ActivityLogMetadata.Serialize(new
                {
                    taskId = task.Id,
                    title = task.Title,
                    pageId = task.PageId,
                    oldStatus = oldStatus.ToString(),
                    newStatus = request.Status.ToString()
                })),
            cancellationToken);

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskStatusChanged",
                WorkspaceId: task.WorkspaceId,
                PageId: task.PageId,
                TaskId: task.Id,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Payload: new
                {
                    task = dto,
                    status = request.Status.ToString()
                }),
            cancellationToken);

        var assigneeIds = dto.Assignees.Select(x => x.UserId).ToArray();

        await _notificationService.NotifyManyAsync(
            assigneeIds,
            NotificationTemplates.TaskStatusChanged(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                task.WorkspaceId,
                task.Id,
                task.Title,
                request.Status),
            excludeUserIds: new[] { currentUserId },
            cancellationToken);

        return Result.Success(dto);
    }

    private static ActivityAction ResolveStatusAction(
        StatusWorkTask oldStatus,
        StatusWorkTask newStatus)
    {
        if (newStatus == StatusWorkTask.Done)
            return ActivityAction.Complete;

        if (oldStatus == StatusWorkTask.Done)
            return ActivityAction.Reopen;

        return ActivityAction.Update;
    }
}
