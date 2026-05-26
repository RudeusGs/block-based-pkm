using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskCreatedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    PriorityWorkTask Priority,
    DateTimeOffset? DueDate,
    Guid[] AssigneeUserIds,
    Guid CreatedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
