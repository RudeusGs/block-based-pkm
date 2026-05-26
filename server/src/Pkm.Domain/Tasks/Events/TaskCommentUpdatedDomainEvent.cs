using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskCommentUpdatedDomainEvent(
    Guid CommentId,
    Guid TaskId,
    Guid UserId,
    string OldContent,
    string NewContent,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
