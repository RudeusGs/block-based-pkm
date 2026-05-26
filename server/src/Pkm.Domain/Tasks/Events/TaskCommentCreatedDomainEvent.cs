using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Tasks.Events;

public sealed record TaskCommentCreatedDomainEvent(
    Guid CommentId,
    Guid TaskId,
    Guid UserId,
    Guid? ParentCommentId,
    string Content,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
