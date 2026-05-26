using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Queries.GetBlock;

public sealed record GetBlockQuery(Guid BlockId) : IQuery<BlockDto>;
