using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;

public sealed class UpdateWorkTaskHandler : ICommandHandler<UpdateWorkTaskCommand, WorkTaskDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IPageReadRepository _pageReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly UpdateWorkTaskCommandValidator _validator;

    public UpdateWorkTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IPageReadRepository pageReadRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        UpdateWorkTaskCommandValidator validator)
    {
        _currentUser = currentUser;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _pageReadRepository = pageReadRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        UpdateWorkTaskCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidUpdateRequest(validationErrors));
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

        if (!TaskPermissionRules.CanManageTasks(access.Role))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskManageForbidden);
        }

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);
        }

        var page = await _pageReadRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.PageNotFound);
        }

        if (page.IsArchived)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.PageArchived);
        }

        if (page.WorkspaceId != task.WorkspaceId)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.PageDifferentWorkspace);
        }

        try
        {
            var now = _clock.UtcNow;

            task.UpdateDetailsAndLocation(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                page.WorkspaceId,
                page.Id,
                currentUserId,
                now);

            _workTaskWriteRepository.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = await _workTaskReadRepository.GetDetailAsync(task.Id, cancellationToken);
            var dto = detail is null ? task.ToDto() : detail.ToDto();

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkTaskDto>(
                new Error(
                    "Task.UpdateFailed",
                    ex.Message,
                    ResultStatus.Unprocessable));
        }
    }
}
