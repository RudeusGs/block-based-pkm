using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed class CreateWorkTaskHandler : ICommandHandler<CreateWorkTaskCommand, WorkTaskDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageReadRepository _pageReadRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CreateWorkTaskCommandValidator _validator;

    public CreateWorkTaskHandler(
        ICurrentUser currentUser,
        IPageReadRepository pageReadRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        CreateWorkTaskCommandValidator validator)
    {
        _currentUser = currentUser;
        _pageReadRepository = pageReadRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
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

        var page = await _pageReadRepository.GetByIdAsync(request.PageId, cancellationToken);
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
                request.DueDate,
                assigneeIds);

            _workTaskWriteRepository.Add(task);

            foreach (var assigneeUserId in assigneeIds)
            {
                _taskAssigneeRepository.Add(
                    TaskAssignee.Create(task.Id, assigneeUserId, now));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = await _workTaskReadRepository.GetDetailAsync(task.Id, cancellationToken);
            var dto = detail is null
                ? task.ToDto()
                : detail.ToDto();

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
