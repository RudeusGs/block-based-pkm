using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskUnassignedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    Guid AssigneeUserId,
    Guid UnassignedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
