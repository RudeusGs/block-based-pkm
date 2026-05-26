using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskUpdatedDomainEvent(
    Guid TaskId,
    Guid WorkspaceId,
    Guid? OldPageId,
    Guid? NewPageId,
    string OldTitle,
    string NewTitle,
    string? OldDescription,
    string? NewDescription,
    PriorityWorkTask OldPriority,
    PriorityWorkTask NewPriority,
    DateTimeOffset? OldDueDate,
    DateTimeOffset? NewDueDate,
    Guid UpdatedByUserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
