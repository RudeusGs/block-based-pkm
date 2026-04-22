namespace Pkm.Api.Contracts.Requests.Pages;

public sealed record CreatePageRequest(
    string Title,
    Guid? ParentPageId = null,
    string? Icon = null,
    string? CoverImage = null);