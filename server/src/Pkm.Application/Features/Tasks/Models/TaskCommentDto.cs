namespace Pkm.Application.Features.Tasks.Models;

public sealed record TaskCommentDto(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    Guid? ParentId,
    string Content,
    bool IsDeleted,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    DateTimeOffset? DeletedDate);