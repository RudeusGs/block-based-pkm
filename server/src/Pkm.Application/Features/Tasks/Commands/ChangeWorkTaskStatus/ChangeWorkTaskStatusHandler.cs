using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed class ChangeWorkTaskStatusHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;
    private readonly ChangeWorkTaskStatusCommandValidator _validator;
    private readonly INotificationService _notificationService;
    public ChangeWorkTaskStatusHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock,
        ChangeWorkTaskStatusCommandValidator validator,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
        _validator = validator;
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

        var task = await _workTaskRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);
        }

        var now = _clock.UtcNow;

        task.ChangeStatus(request.Status, currentUserId, now);
        _workTaskRepository.Update(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

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
}
