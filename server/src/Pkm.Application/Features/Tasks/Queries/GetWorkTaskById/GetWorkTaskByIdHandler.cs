using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;

namespace Pkm.Application.Features.Tasks.Queries.GetWorkTaskById;

public sealed class GetWorkTaskByIdHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IWorkTaskRepository _workTaskRepository;

    public GetWorkTaskByIdHandler(
        ICurrentUser currentUser,
        ITaskAccessEvaluator taskAccessEvaluator,
        IWorkTaskRepository workTaskRepository)
    {
        _currentUser = currentUser;
        _taskAccessEvaluator = taskAccessEvaluator;
        _workTaskRepository = workTaskRepository;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        GetWorkTaskByIdQuery request,
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

        if (!access.CanReadTask)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        var detail = await _workTaskRepository.GetDetailAsync(request.TaskId, cancellationToken);
        if (detail is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        return Result.Success(detail.ToDto());
    }
}