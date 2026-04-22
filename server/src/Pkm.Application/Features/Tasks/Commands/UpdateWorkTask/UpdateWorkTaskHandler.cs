using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.Common;

namespace Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;

public sealed class UpdateWorkTaskHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;

    public UpdateWorkTaskHandler(
        ICurrentUser currentUser,
        IWorkTaskRepository workTaskRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IPageRepository pageRepository,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock)
    {
        _currentUser = currentUser;
        _workTaskRepository = workTaskRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        UpdateWorkTaskCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (request.TaskId == Guid.Empty)
            errors.Add("TaskId không hợp lệ.");

        if (request.PageId == Guid.Empty)
            errors.Add("PageId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Tiêu đề task không được để trống.");

        if (errors.Count > 0)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidUpdateRequest(errors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        if (!access.CanEditTask)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        var task = await _workTaskRepository.GetByIdForUpdateAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskNotFound);

        var page = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.PageNotFound);

        if (page.IsArchived)
            return Result.Failure<WorkTaskDto>(TaskErrors.PageArchived);

        if (page.WorkspaceId != task.WorkspaceId)
            return Result.Failure<WorkTaskDto>(TaskErrors.PageDifferentWorkspace);

        try
        {
            var now = _clock.UtcNow;

            task.UpdateDetails(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDate,
                currentUserId,
                now);

            if (task.PageId != page.Id)
            {
                task.ChangeLocation(
                    page.WorkspaceId,
                    page.Id,
                    currentUserId,
                    now);
            }

            _workTaskRepository.Update(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = await _workTaskRepository.GetDetailAsync(task.Id, cancellationToken);
            var dto = detail is null ? task.ToDto() : detail.ToDto();

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskUpdated",
                    WorkspaceId: task.WorkspaceId,
                    PageId: task.PageId,
                    TaskId: task.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: dto),
                cancellationToken);

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