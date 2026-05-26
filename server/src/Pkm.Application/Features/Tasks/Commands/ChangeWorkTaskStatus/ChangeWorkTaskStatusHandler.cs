using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
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
    private readonly IClock _clock;
    private readonly ChangeWorkTaskStatusCommandValidator _validator;

    public ChangeWorkTaskStatusHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        ChangeWorkTaskStatusCommandValidator validator)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
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

        task.ChangeStatus(request.Status, currentUserId, now);
        _workTaskWriteRepository.Update(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = await _workTaskReadRepository.GetDetailAsync(task.Id, cancellationToken);
        var dto = detail is null ? task.ToDto() : detail.ToDto();

        return Result.Success(dto);
    }
}
