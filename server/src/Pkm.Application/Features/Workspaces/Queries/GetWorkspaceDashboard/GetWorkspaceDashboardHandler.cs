using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Workspaces.Queries.GetWorkspaceDashboard;

public sealed class GetWorkspaceDashboardHandler : IQueryHandler<GetWorkspaceDashboardQuery, WorkspaceDashboardDto>
{
    private const int RecentPagesLimit = 6;
    private const int MyOpenTasksLimit = 6;
    private const int LatestActivitiesLimit = 10;
    private const int MembersLimit = 8;

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageReadRepository _pageRepository;
    private readonly IWorkTaskReadRepository _taskRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public GetWorkspaceDashboardHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageReadRepository pageRepository,
        IWorkTaskReadRepository taskRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IActivityLogRepository activityLogRepository)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageRepository = pageRepository;
        _taskRepository = taskRepository;
        _taskAssigneeRepository = taskAssigneeRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<Result<WorkspaceDashboardDto>> HandleAsync(
        GetWorkspaceDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDashboardDto>(
                WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDashboardDto>(
                WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceDashboardDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<WorkspaceDashboardDto>(
                WorkspaceErrors.WorkspaceForbidden);
        }

        var workspace = await _workspaceRepository.GetDetailAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceDashboardDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        var recentPages = await _pageRepository.ListRecentlyUpdatedByWorkspaceAsync(
            request.WorkspaceId,
            pageNumber: 1,
            pageSize: RecentPagesLimit,
            cancellationToken);

        var pageCount = await _pageRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        var myTaskFilter = new WorkTaskListFilter(
            IncludeCompleted: false,
            PageNumber: 1,
            PageSize: MyOpenTasksLimit);

        var myOpenTasks = await _taskRepository.ListAssignedToUserAsync(
            currentUserId,
            request.WorkspaceId,
            myTaskFilter,
            cancellationToken);

        var myOpenTaskCount = await _taskRepository.CountAssignedToUserAsync(
            currentUserId,
            request.WorkspaceId,
            myTaskFilter,
            cancellationToken);

        var workspaceOpenTaskCount = await _taskRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            new WorkTaskListFilter(IncludeCompleted: false),
            cancellationToken);

        var latestActivities = await _activityLogRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            action: null,
            entityType: null,
            userId: null,
            fromUtc: null,
            toUtc: null,
            search: null,
            pageNumber: 1,
            pageSize: LatestActivitiesLimit,
            cancellationToken);

        var members = await _workspaceMemberRepository.ListByWorkspacePagedAsync(
            request.WorkspaceId,
            pageNumber: 1,
            pageSize: MembersLimit,
            cancellationToken);

        var memberCount = await _workspaceMemberRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        var assigneeMap = await _taskAssigneeRepository.ListUserIdsByTaskIdsAsync(
            myOpenTasks.Select(x => x.Id).ToArray(),
            cancellationToken);

        var dto = new WorkspaceDashboardDto(
            Workspace: new WorkspaceDto(
                workspace.Id,
                workspace.Name,
                workspace.Description,
                workspace.AvatarUrl,
                workspace.Visibility,
                workspace.OwnerId,
                workspace.LastModifiedBy,
                workspace.CreatedDate,
                workspace.UpdatedDate,
                access.Role,
                access.CanRead,
                access.CanWrite,
                access.CanManageMembers),
            Stats: new WorkspaceDashboardStatsDto(
                PageCount: pageCount,
                OpenTaskCount: workspaceOpenTaskCount,
                MyOpenTaskCount: myOpenTaskCount,
                MemberCount: memberCount),
            RecentPages: recentPages.Select(x => x.ToDto()).ToArray(),
            MyOpenTasks: myOpenTasks.Select(x =>
            {
                assigneeMap.TryGetValue(x.Id, out var assignees);
                return x.ToDto(assignees ?? Array.Empty<Guid>());
            }).ToArray(),
            LatestActivities: latestActivities,
            Members: members.Select(member => member.ToDto(currentUserId)).ToArray());

        return Result.Success(dto);
    }
}
