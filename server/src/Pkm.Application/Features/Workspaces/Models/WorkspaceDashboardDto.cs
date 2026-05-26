using Pkm.Application.Features.Activity.Models;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceDashboardDto(
    WorkspaceDto Workspace,
    WorkspaceDashboardStatsDto Stats,
    IReadOnlyList<PageDto> RecentPages,
    IReadOnlyList<WorkTaskDto> MyOpenTasks,
    IReadOnlyList<ActivityLogDto> LatestActivities,
    IReadOnlyList<WorkspaceMemberDto> Members);

public sealed record WorkspaceDashboardStatsDto(
    int PageCount,
    int OpenTaskCount,
    int MyOpenTaskCount,
    int MemberCount);
