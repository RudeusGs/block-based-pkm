namespace Pkm.Api.Contracts.Requests.Pages;

public sealed record UpdatePageMetadataRequest(
    string Title,
    string? Icon = null,
    string? CoverImage = null);