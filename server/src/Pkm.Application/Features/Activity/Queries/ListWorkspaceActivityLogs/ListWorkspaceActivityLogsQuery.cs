using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Models;

namespace Pkm.Application.Features.Activity.Queries.ListWorkspaceActivityLogs;

public sealed record ListWorkspaceActivityLogsQuery(
    Guid WorkspaceId,
    string? Action,
    string? EntityType,
    Guid? UserId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Search,
    int PageNumber,
    int PageSize) : IQuery<ActivityLogPagedResultDto>;
