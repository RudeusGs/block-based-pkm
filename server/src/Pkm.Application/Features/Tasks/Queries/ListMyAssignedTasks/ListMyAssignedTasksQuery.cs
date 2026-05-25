using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Queries.ListMyAssignedTasks;

public sealed record ListMyAssignedTasksQuery(
    Guid? WorkspaceId = null,
    string? Keyword = null,
    StatusWorkTask? Status = null,
    PriorityWorkTask? Priority = null,
    DateTimeOffset? DueFrom = null,
    DateTimeOffset? DueTo = null,
    bool IncludeCompleted = true,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<WorkTaskPagedResultDto>;
