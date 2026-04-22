using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Models;

public static class WorkTaskMappings
{
    public static WorkTaskDto ToDto(this WorkTaskDetailReadModel readModel)
        => new(
            readModel.Id,
            readModel.WorkspaceId,
            readModel.PageId,
            readModel.Title,
            readModel.Description,
            readModel.Status,
            readModel.Priority,
            readModel.DueDate,
            readModel.CreatedById,
            readModel.LastModifiedById,
            readModel.CreatedDate,
            readModel.UpdatedDate,
            readModel.Assignees
                .Select(x => new TaskAssigneeDto(x.UserId))
                .ToArray());

    public static WorkTaskDto ToDto(
        this WorkTaskListItemReadModel readModel,
        IReadOnlyList<Guid> assigneeUserIds)
        => new(
            readModel.Id,
            readModel.WorkspaceId,
            readModel.PageId,
            readModel.Title,
            readModel.Description,
            readModel.Status,
            readModel.Priority,
            readModel.DueDate,
            readModel.CreatedById,
            readModel.LastModifiedById,
            readModel.CreatedDate,
            readModel.UpdatedDate,
            assigneeUserIds
                .Select(x => new TaskAssigneeDto(x))
                .ToArray());

    public static WorkTaskDto ToDto(
        this WorkTask entity,
        IReadOnlyList<TaskAssigneeReadModel>? assignees = null)
        => new(
            entity.Id,
            entity.WorkspaceId,
            entity.PageId,
            entity.Title,
            entity.Description,
            entity.Status,
            entity.Priority,
            entity.DueDate,
            entity.CreatedById,
            entity.LastModifiedById,
            entity.CreatedDate,
            entity.UpdatedDate,
            assignees is null
                ? Array.Empty<TaskAssigneeDto>()
                : assignees.Select(x => new TaskAssigneeDto(x.UserId)).ToArray());
}