using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Models;

public sealed record WorkTaskListFilter(
    string? Keyword = null,
    StatusWorkTask? Status = null,
    PriorityWorkTask? Priority = null,
    Guid? AssigneeUserId = null,
    DateTimeOffset? DueFrom = null,
    DateTimeOffset? DueTo = null,
    bool IncludeCompleted = true,
    int PageNumber = 1,
    int PageSize = 20);