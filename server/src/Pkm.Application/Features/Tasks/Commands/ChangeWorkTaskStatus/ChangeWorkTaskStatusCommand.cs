using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.ChangeWorkTaskStatus;

public sealed record ChangeWorkTaskStatusCommand(
    Guid TaskId,
    StatusWorkTask Status) : ICommand<WorkTaskDto>;
