namespace Pkm.Application.Features.Pages.Models;

public sealed record PagePublishDto(
    Guid PageId,
    bool IsPublished,
    string? PublicToken,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy);
