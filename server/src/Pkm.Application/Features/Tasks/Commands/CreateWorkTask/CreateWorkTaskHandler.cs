using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Domain.Audit;
namespace Pkm.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskHandler : ICommandHandler<CreateWorkTaskCommand, WorkTaskDto>
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
    private readonly CreateWorkTaskCommandValidator _validator;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;
    public CreateWorkTaskHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IUnitOfWork unitOfWork,
        ITaskRealtimePublisher taskRealtimePublisher,
        IClock clock,
        CreateWorkTaskCommandValidator validator,
        INotificationService notificationService,
        IActivityLogService activityLogService)
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
        _validator = validator;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkTaskDto>> HandleAsync(
        CreateWorkTaskCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.InvalidCreateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.MissingUserContext);
        }

        var page = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.PageNotFound);
        }

        if (page.IsArchived)
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.PageArchived);
        }

        var workspaceAccess = await _workspaceAccessEvaluator.EvaluateAsync(
            page.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!workspaceAccess.Exists)
        {
            return Result.Failure<WorkTaskDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!TaskPermissionRules.CanManageTasks(workspaceAccess.Role))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.TaskManageForbidden);
        }

        var assigneeIds = (request.AssigneeUserIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (assigneeIds.Contains(currentUserId))
        {
            return Result.Failure<WorkTaskDto>(TaskErrors.CannotAssignTaskToSelf);
        }
        if (assigneeIds.Length > 0)
        {
            var membersList = await _workspaceMemberRepository.ListByWorkspaceAsync(
                page.WorkspaceId,
                cancellationToken);

            var existingAssigneeIds = membersList
                .Where(m => assigneeIds.Contains(m.UserId))
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            if (existingAssigneeIds.Count != assigneeIds.Length)
            {
                return Result.Failure<WorkTaskDto>(TaskErrors.AssigneeNotInWorkspace);
            }
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

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    task.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.WorkTask,
                    task.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã tạo task \"{task.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        taskId = task.Id,
                        title = task.Title,
                        pageId = task.PageId,
                        priority = task.Priority.ToString(),
                        dueDate = task.DueDate,
                        assigneeUserIds = assigneeIds
                    })),
                cancellationToken);

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
            await _notificationService.NotifyWorkspaceAsync(
                page.WorkspaceId,
                NotificationTemplates.TaskCreated(
                    currentUserId,
                    _currentUser.UserName ?? "Có người",
                    page.WorkspaceId,
                    task.Id,
                    task.Title),
                excludeUserIds: new[] { currentUserId }.Concat(assigneeIds),
                cancellationToken);

            await _notificationService.NotifyManyAsync(
                assigneeIds,
                NotificationTemplates.TaskAssigned(
                    currentUserId,
                    _currentUser.UserName ?? "Có người",
                    task.WorkspaceId,
                    task.Id,
                    task.Title),
                excludeUserIds: new[] { currentUserId },
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
