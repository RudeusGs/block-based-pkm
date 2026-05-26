using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Policies;

namespace Pkm.Application.Features.Tasks.Commands.DeleteWorkTask;

public sealed class DeleteWorkTaskHandler : ICommandHandler<DeleteWorkTaskCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly DeleteWorkTaskCommandValidator _validator;

    public DeleteWorkTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        DeleteWorkTaskCommandValidator validator)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
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

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure(TaskErrors.TaskNotFound);
        }

        var now = _clock.UtcNow;
        var detailBeforeDelete = await _workTaskReadRepository.GetDetailAsync(
            task.Id,
            cancellationToken);

        var assigneeIds = detailBeforeDelete?.Assignees
            .Select(x => x.UserId)
            .ToArray() ?? Array.Empty<Guid>();

        task.Delete(currentUserId, now, assigneeIds);
        _workTaskWriteRepository.Update(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
