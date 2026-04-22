using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.CreateWorkTask;

public sealed record CreateWorkTaskCommand(
    Guid PageId,
    string Title,
    string? Description,
    PriorityWorkTask Priority,
    DateTimeOffset? DueDate,
    IReadOnlyList<Guid>? AssigneeUserIds = null);