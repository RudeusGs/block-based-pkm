using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Application.Features.Documents.Queries.GetPagePresence;

public sealed record GetPagePresenceQuery(Guid PageId) : IQuery<PagePresenceDto>;
