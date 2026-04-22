using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Tasks.Queries.ListWorkspaceTasks;

public sealed class ListWorkspaceTasksHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;

    public ListWorkspaceTasksHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
    }

    public async Task<Result<WorkTaskPagedResultDto>> HandleAsync(
        ListWorkspaceTasksQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<WorkTaskPagedResultDto>(TaskErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkTaskPagedResultDto>(TaskErrors.MissingUserContext);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<WorkTaskPagedResultDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanRead)
            return Result.Failure<WorkTaskPagedResultDto>(WorkspaceErrors.WorkspaceForbidden);

        var filter = new WorkTaskListFilter(
            request.Keyword,
            request.Status,
            request.Priority,
            request.AssigneeUserId,
            request.DueFrom,
            request.DueTo,
            request.IncludeCompleted,
            request.PageNumber,
            request.PageSize);

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 100);

        var items = await _workTaskRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            filter with { PageNumber = pageNumber, PageSize = pageSize },
            cancellationToken);

        var totalCount = await _workTaskRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            filter,
            cancellationToken);

        var taskIds = items.Select(x => x.Id).ToArray();
        var assigneeMap = await _taskAssigneeRepository.ListUserIdsByTaskIdsAsync(taskIds, cancellationToken);

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