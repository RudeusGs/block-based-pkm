using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Pages.Commands.UnfavoritePage;

public sealed record UnfavoritePageCommand(Guid PageId) : ICommand;
