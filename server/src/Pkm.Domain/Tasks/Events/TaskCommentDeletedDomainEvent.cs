using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskCommentDeletedDomainEvent(
    Guid CommentId,
    Guid TaskId,
    Guid CommentOwnerUserId,
    Guid DeletedByUserId,
    bool DeletedByModeration,
    string OldContent,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
