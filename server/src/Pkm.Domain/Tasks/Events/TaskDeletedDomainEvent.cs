using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskDeletedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? PageId,
    string TaskTitle,
    Guid[] AssigneeUserIds,
    Guid DeletedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
