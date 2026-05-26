using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Queries.ListPageBlocks;

public sealed record ListPageBlocksQuery(Guid PageId) : IQuery<PageDocumentDto>;
