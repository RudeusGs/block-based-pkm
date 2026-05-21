namespace Pkm.Api.Contracts.Requests.Pages;

public sealed record UpdatePageMetadataRequest(
    long ExpectedRevision,
    string Title,
    string? Icon = null,
    string? CoverImage = null);