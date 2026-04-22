namespace Pkm.Api.Contracts.Requests.Tasks;

public sealed record UpdateWorkTaskRequest(
    Guid PageId,
    string Title,
    string? Description,
    string Priority,
    DateTimeOffset? DueDate);