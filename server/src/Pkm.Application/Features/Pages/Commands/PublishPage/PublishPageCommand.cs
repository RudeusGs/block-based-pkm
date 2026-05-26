using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.PublishPage;

public sealed record PublishPageCommand(Guid PageId) : ICommand<PagePublishDto>;
