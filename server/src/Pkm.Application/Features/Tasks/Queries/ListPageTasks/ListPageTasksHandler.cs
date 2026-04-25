using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Tasks.Queries.ListPageTasks;

public sealed class ListPageTasksHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly ListPageTasksQueryValidator _validator;

    public ListPageTasksHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IWorkTaskRepository workTaskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        ListPageTasksQueryValidator validator)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _workTaskRepository = workTaskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _validator = validator;
    }

    public async Task<Result<WorkTaskPagedResultDto>> HandleAsync(
        ListPageTasksQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkTaskPagedResultDto>(TaskErrors.InvalidListRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkTaskPagedResultDto>(PageErrors.MissingUserContext);
        }

        var pageAccess = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!pageAccess.Exists)
        {
            return Result.Failure<WorkTaskPagedResultDto>(TaskErrors.PageNotFound);
        }

        if (!pageAccess.CanRead)
        {
            return Result.Failure<WorkTaskPagedResultDto>(TaskErrors.TaskForbidden);
        }

        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;

        var filter = new WorkTaskListFilter(
            request.Keyword,
            request.Status,
            request.Priority,
            request.AssigneeUserId,
            request.DueFrom,
            request.DueTo,
            request.IncludeCompleted,
            pageNumber,
            pageSize);

        var items = await _workTaskRepository.ListByPageAsync(
            request.PageId,
            filter,
            cancellationToken);

        var totalCount = await _workTaskRepository.CountByPageAsync(
            request.PageId,
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
