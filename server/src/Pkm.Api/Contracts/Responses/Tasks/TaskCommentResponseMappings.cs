using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Api.Contracts.Responses.Tasks;

public static class TaskCommentResponseMappings
{
    public static TaskCommentResponse ToResponse(this TaskCommentDto dto)
        => new(
            dto.Id,
            dto.TaskId,
            dto.UserId,
            dto.ParentId,
            dto.Content,
            dto.IsDeleted,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.DeletedDate);

    public static TaskCommentPagedResultResponse ToResponse(this TaskCommentPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}