using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Models;

public sealed record WorkTaskDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? PageId,
    string Title,
    string? Description,
    StatusWorkTask Status,
    PriorityWorkTask Priority,
    DateTimeOffset? DueDate,
    Guid CreatedById,
    Guid? LastModifiedById,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    IReadOnlyList<TaskAssigneeDto> Assignees);