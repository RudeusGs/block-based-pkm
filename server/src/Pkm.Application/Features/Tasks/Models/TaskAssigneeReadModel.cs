namespace Pkm.Application.Features.Tasks.Models;

public sealed record TaskAssigneeReadModel(
    Guid TaskId,
    Guid UserId,
    DateTimeOffset AssignedAtUtc);