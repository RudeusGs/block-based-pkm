using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.DuplicatePage;

public sealed record DuplicatePageCommand(Guid PageId) : ICommand<PageDto>;
