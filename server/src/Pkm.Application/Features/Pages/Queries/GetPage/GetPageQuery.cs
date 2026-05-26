using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.GetPage;

public sealed record GetPageQuery(Guid PageId) : IQuery<PageDto>;
