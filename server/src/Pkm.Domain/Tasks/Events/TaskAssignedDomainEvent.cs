using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskAssignedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    Guid AssigneeUserId,
    Guid AssignedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
