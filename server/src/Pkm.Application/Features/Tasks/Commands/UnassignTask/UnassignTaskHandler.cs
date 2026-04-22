using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;

namespace Pkm.Application.Features.Tasks.Commands.UnassignTask;

public sealed class UnassignTaskHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;

    public UnassignTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        UnassignTaskCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TaskId == Guid.Empty)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidTaskId(request.TaskId));

        if (request.AssigneeUserId == Guid.Empty)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidAssigneeUserId(request.AssigneeUserId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);

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

        var assignee = await _taskAssigneeRepository.GetByTaskAndUserAsync(
            request.TaskId,
            request.AssigneeUserId,
            cancellationToken);

        if (assignee is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.AssigneeNotFound);

        var now = _clock.UtcNow;

        task.RecordAssignmentChange(currentUserId, now);
        _workTaskRepository.Update(task);

        _taskAssigneeRepository.Remove(assignee);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

        await _taskRealtimePublisher.PublishToPageAsync(
            new TaskRealtimeEnvelope(
                EventName: "TaskUnassigned",
                WorkspaceId: task.WorkspaceId,
                PageId: task.PageId,
                TaskId: task.Id,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Payload: dto),
            cancellationToken);

        return Result.Success(dto);
    }
}