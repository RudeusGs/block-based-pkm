using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListSubPages;

public sealed record ListSubPagesQuery(Guid ParentPageId) : IQuery<IReadOnlyList<PageDto>>;
