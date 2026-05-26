using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.GetPublishedPageDocument;

public sealed record GetPublishedPageDocumentQuery(
    string PublicToken,
    int PageNumber = 1,
    int PageSize = 200) : IQuery<PublishedPageDocumentDto>;
