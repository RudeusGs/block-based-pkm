using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Queries.ListPageBlocks;

public sealed record ListPageBlocksQuery(
    Guid PageId,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PageDocumentDto>;
