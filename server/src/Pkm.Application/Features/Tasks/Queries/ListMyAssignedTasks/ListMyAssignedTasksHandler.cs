using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Tasks.Queries.ListMyAssignedTasks;

public sealed class ListMyAssignedTasksHandler : IQueryHandler<ListMyAssignedTasksQuery, WorkTaskPagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkTaskReadRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly ListMyAssignedTasksQueryValidator _validator;

    public ListMyAssignedTasksHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkTaskReadRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ListMyAssignedTasksQueryValidator validator)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _validator = validator;
    }

    public async Task<Result<WorkTaskPagedResultDto>> HandleAsync(
        ListMyAssignedTasksQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkTaskPagedResultDto>(
                TaskErrors.InvalidListRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkTaskPagedResultDto>(
                TaskErrors.MissingUserContext);
        }

        if (request.WorkspaceId.HasValue)
        {
            var access = await _workspaceAccessEvaluator.EvaluateAsync(
                request.WorkspaceId.Value,
                currentUserId,
                cancellationToken);

            if (!access.Exists)
            {
                return Result.Failure<WorkTaskPagedResultDto>(
                    WorkspaceErrors.WorkspaceNotFound);
            }

            if (!access.CanRead)
            {
                return Result.Failure<WorkTaskPagedResultDto>(
                    WorkspaceErrors.WorkspaceForbidden);
            }
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;

        var filter = new WorkTaskListFilter(
            request.Keyword,
            request.Status,
            request.Priority,
            AssigneeUserId: null,
            request.DueFrom,
            request.DueTo,
            request.IncludeCompleted,
            pageNumber,
            pageSize);

        var items = await _workTaskRepository.ListAssignedToUserAsync(
            currentUserId,
            request.WorkspaceId,
            filter,
            cancellationToken);

        var totalCount = await _workTaskRepository.CountAssignedToUserAsync(
            currentUserId,
            request.WorkspaceId,
            filter,
            cancellationToken);

        var taskIds = items.Select(x => x.Id).ToArray();
        var assigneeMap = await _taskAssigneeRepository.ListUserIdsByTaskIdsAsync(
            taskIds,
            cancellationToken);

        var dto = new WorkTaskPagedResultDto(
            items.Select(x =>
            {
                assigneeMap.TryGetValue(x.Id, out var assignees);
                return x.ToDto(assignees ?? Array.Empty<Guid>());
            }).ToArray(),
            pageNumber,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));

        return Result.Success(dto);
    }
}
