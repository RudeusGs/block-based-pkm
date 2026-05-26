using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.FavoritePage;

public sealed record FavoritePageCommand(Guid PageId) : ICommand<PageDto>;
