using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.RestorePage;

public sealed record RestorePageCommand(Guid PageId) : ICommand<PageDto>;
