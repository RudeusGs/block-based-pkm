using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Pages.Commands.DeletePage;

public sealed record DeletePageCommand(Guid PageId) : ICommand;
