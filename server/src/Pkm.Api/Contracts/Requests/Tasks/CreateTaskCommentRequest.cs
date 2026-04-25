namespace Pkm.Api.Contracts.Requests.Tasks;

public sealed record CreateTaskCommentRequest(
    string Content,
    Guid? ParentId = null);