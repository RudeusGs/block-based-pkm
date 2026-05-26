using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskCommentRestoredDomainEvent(
    Guid CommentId,
    Guid TaskId,
    Guid UserId,
    string Content,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
