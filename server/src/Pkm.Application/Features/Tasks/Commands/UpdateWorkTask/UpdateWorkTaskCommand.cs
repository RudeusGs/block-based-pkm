using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.UpdateWorkTask;

public sealed record UpdateWorkTaskCommand(
    Guid TaskId,
    Guid PageId,
    string Title,
    string? Description,
    PriorityWorkTask Priority,
    DateTimeOffset? DueDate);