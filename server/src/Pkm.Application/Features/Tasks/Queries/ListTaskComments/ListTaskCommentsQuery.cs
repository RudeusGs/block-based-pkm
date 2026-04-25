namespace Pkm.Application.Features.Tasks.Queries.ListTaskComments;

public sealed record ListTaskCommentsQuery(
    Guid TaskId,
    int PageNumber = 1,
    int PageSize = 10,
    bool IncludeDeleted = true);