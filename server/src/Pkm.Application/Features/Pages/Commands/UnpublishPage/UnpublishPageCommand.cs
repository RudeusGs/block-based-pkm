using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.UnpublishPage;

public sealed record UnpublishPageCommand(Guid PageId) : ICommand<PagePublishDto>;
