using Pkm.Application.Features.Tasks.Models;

namespace Pkm.Api.Contracts.Responses.Tasks;

public static class WorkTaskResponseMappings
{
    public static WorkTaskAssigneeResponse ToResponse(this TaskAssigneeDto dto)
        => new(dto.UserId);

    public static WorkTaskResponse ToResponse(this WorkTaskDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.PageId,
            dto.Title,
            dto.Description,
            dto.Status.ToString(),
            dto.Priority.ToString(),
            dto.DueDate,
            dto.CreatedById,
            dto.LastModifiedById,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.Assignees.Select(x => x.ToResponse()).ToArray());

    public static WorkTaskPagedResultResponse ToResponse(this WorkTaskPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}
