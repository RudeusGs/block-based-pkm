using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskStatusChangedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    StatusWorkTask OldStatus,
    StatusWorkTask NewStatus,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
