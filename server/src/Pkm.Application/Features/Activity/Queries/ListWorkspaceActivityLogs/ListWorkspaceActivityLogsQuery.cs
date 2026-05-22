namespace Pkm.Application.Features.Activity.Queries.ListWorkspaceActivityLogs;

public sealed record ListWorkspaceActivityLogsQuery(
    Guid WorkspaceId,
    string? Action,
    string? EntityType,
    int PageNumber,
    int PageSize);
