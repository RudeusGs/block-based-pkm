using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Commands.CreatePage;

public sealed record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentPageId = null,
    string? Icon = null,
    string? CoverImage = null) : ICommand<PageDto>;
