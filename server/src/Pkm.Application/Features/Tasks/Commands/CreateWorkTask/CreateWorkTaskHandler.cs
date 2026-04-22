using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IClock _clock;

    public CreateWorkTaskHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _unitOfWork = unitOfWork;
        _taskRealtimePublisher = taskRealtimePublisher;
        _clock = clock;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        CreateWorkTaskCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (request.PageId == Guid.Empty)
            errors.Add("PageId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Tiêu đề task không được để trống.");

        if (errors.Count > 0)
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidCreateRequest(errors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);

        var page = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<WorkTaskDto>(TaskErrors.PageNotFound);

        if (page.IsArchived)
            return Result.Failure<WorkTaskDto>(TaskErrors.PageArchived);

        var workspaceAccess = await _workspaceAccessEvaluator.EvaluateAsync(
            page.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!workspaceAccess.Exists)
            return Result.Failure<WorkTaskDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!workspaceAccess.CanCreateTasks)
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskForbidden);

        var assigneeIds = (request.AssigneeUserIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var assigneeUserId in assigneeIds)
        {
            var exists = await _workspaceMemberRepository.ExistsAsync(
                page.WorkspaceId,
                assigneeUserId,
                cancellationToken);

            if (!exists)
                return Result.Failure<WorkTaskDto>(TaskErrors.AssigneeNotInWorkspace);
        }

        try
        {
            var now = _clock.UtcNow;

            var task = WorkTask.Create(
                Guid.NewGuid(),
                request.Title,
                page.WorkspaceId,
                currentUserId,
                now,
                page.Id,
                request.Priority,
                request.Description,
                request.DueDate);

            _workTaskRepository.Add(task);

            foreach (var assigneeUserId in assigneeIds)
            {
                _taskAssigneeRepository.Add(
                    TaskAssignee.Create(task.Id, assigneeUserId, now));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = await _workTaskRepository.GetDetailAsync(task.Id, cancellationToken);
            var dto = detail is null
                ? task.ToDto()
                : detail.ToDto();

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskCreated",
                    WorkspaceId: page.WorkspaceId,
                    PageId: page.Id,
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
                    "Task.CreateFailed",
                    ex.Message,
                    ResultStatus.Unprocessable));
        }
    }
}