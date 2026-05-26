using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Queries.GetBlockLease;

public sealed record GetBlockLeaseQuery(Guid BlockId) : IQuery<BlockLeaseDto>;
