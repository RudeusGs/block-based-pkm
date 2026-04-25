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

namespace Pkm.Application.Features.Tasks.Commands.AssignTask;

public sealed class AssignTaskHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    public AssignTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock, 
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
        _notificationService = notificationService;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        AssignTaskCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TaskId == Guid.Empty)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidTaskId(request.TaskId));

        if (request.AssigneeUserId == Guid.Empty)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidAssigneeUserId(request.AssigneeUserId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);

        if (request.AssigneeUserId == currentUserId)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.CannotAssignTaskToSelf);
        }
        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        if (!access.CanAssignTask)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        var task = await _workTaskRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        var existsInWorkspace = await _workspaceMemberRepository.ExistsAsync(
            task.WorkspaceId,
            request.AssigneeUserId,
            cancellationToken);

        if (!existsInWorkspace)
            return Result.Failure<WorkTaskDto>(TaskErrors.AssigneeNotInWorkspace);

        var alreadyAssigned = await _taskAssigneeRepository.ExistsAsync(
            task.Id,
            request.AssigneeUserId,
            cancellationToken);

        if (alreadyAssigned)
            return Result.Failure<WorkTaskDto>(TaskErrors.AssigneeAlreadyExists);

        var now = _clock.UtcNow;

        task.RecordAssignmentChange(currentUserId, now);
        _workTaskRepository.Update(task);

        _taskAssigneeRepository.Add(TaskAssignee.Create(task.Id, request.AssigneeUserId, now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskAssigned",
                WorkspaceId: task.WorkspaceId,
                PageId: task.PageId,
                TaskId: task.Id,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Payload: dto),
            cancellationToken);

        await _notificationService.NotifyAsync(
            request.AssigneeUserId,
            NotificationTemplates.TaskAssigned(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                task.WorkspaceId,
                task.Id,
                task.Title),
            cancellationToken);
        return Result.Success(dto);
    }
}