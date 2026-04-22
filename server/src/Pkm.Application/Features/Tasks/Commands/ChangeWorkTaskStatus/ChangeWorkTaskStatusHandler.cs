using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed class ChangeWorkTaskStatusHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;

    public ChangeWorkTaskStatusHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        ChangeWorkTaskStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TaskId == Guid.Empty)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidTaskId(request.TaskId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        var needsCompleteCapability = request.Status == StatusWorkTask.Done;
        if (needsCompleteCapability && !access.CanCompleteTask)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        if (!needsCompleteCapability && !access.CanEditTask)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        var task = await _workTaskRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

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
                Payload: dto),
            cancellationToken);

        return Result.Success(dto);
    }
}