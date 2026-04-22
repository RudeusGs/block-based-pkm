using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed record ChangeWorkTaskStatusCommand(
    Guid TaskId,
    StatusWorkTask Status);