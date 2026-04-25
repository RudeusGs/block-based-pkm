using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Models;

public static class TaskCommentMappings
{
    public static TaskCommentDto ToDto(this TaskComment entity)
        => new(
            entity.Id,
            entity.TaskId,
            entity.UserId,
            entity.ParentId,
            entity.Content,
            entity.IsDeleted,
            entity.CreatedDate,
            entity.UpdatedDate,
            entity.DeletedDate);
}