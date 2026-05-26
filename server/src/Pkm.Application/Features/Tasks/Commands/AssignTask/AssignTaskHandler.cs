using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.AssignTask;

public sealed class AssignTaskHandler : ICommandHandler<AssignTaskCommand, WorkTaskDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AssignTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
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

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
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

        task.AssignTo(request.AssigneeUserId, currentUserId, now);
        _workTaskWriteRepository.Update(task);

        _taskAssigneeRepository.Add(TaskAssignee.Create(task.Id, request.AssigneeUserId, now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskReadRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

        return Result.Success(dto);
    }
}
