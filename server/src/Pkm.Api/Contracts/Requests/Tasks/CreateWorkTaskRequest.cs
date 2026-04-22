namespace Pkm.Api.Contracts.Requests.Tasks;

public sealed record CreateWorkTaskRequest(
    string Title,
    string? Description,
    string Priority,
    DateTimeOffset? DueDate,
    IReadOnlyList<Guid>? AssigneeUserIds = null);