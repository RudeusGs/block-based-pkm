using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Pages;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Pages;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Queries.GetPagePresence;
using Pkm.Application.Features.Pages.Commands.CreatePage;
using Pkm.Application.Features.Pages.Commands.DeletePage;
using Pkm.Application.Features.Pages.Queries.ListRecentPages;
using Pkm.Application.Features.Pages.Queries.ListFavoritePages;
using Pkm.Application.Features.Pages.Queries.ListArchivedPages;
using Pkm.Application.Features.Pages.Commands.UnfavoritePage;
using Pkm.Application.Features.Pages.Commands.RestorePage;
using Pkm.Application.Features.Pages.Commands.FavoritePage;
using Pkm.Application.Features.Pages.Commands.DuplicatePage;
using Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Queries.GetPage;
using Pkm.Application.Features.Pages.Queries.ListSubPages;
using Pkm.Application.Features.Pages.Queries.ListWorkspacePages;
using Pkm.Application.Features.Pages.Queries.SearchPages;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class PagesController : BaseController
{
    public PagesController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser, dispatcher)
    {
    }

    [HttpPost("api/v1/workspaces/{workspaceId:guid}/pages")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> Create(
        [FromRoute] Guid workspaceId,
        [FromBody] CreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePageCommand(
            workspaceId,
            request.Title,
            request.ParentPageId,
            request.Icon,
            request.CoverImage);

        var result = await ExecuteAsync<CreatePageCommand, PageDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> UpdateMetadata(
        [FromRoute] Guid pageId,
        [FromBody] UpdatePageMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePageMetadataCommand(
                pageId,
                request.ExpectedRevision,
                request.Title,
                request.Icon,
                request.CoverImage);

        var result = await ExecuteAsync<UpdatePageMetadataCommand, PageDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Delete(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            new DeletePageCommand(pageId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PageResponse>>> GetById(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await QueryAsync<GetPageQuery, PageDto>(
            new GetPageQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/presence")]
    [ProducesResponseType(typeof(ApiResult<PagePresenceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePresenceResponse>>> GetPresence(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await QueryAsync<GetPagePresenceQuery, PagePresenceDto>(
            new GetPagePresenceQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/pages")]
    [ProducesResponseType(typeof(ApiResult<PagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePagedResultResponse>>> ListByWorkspace(
        [FromRoute] Guid workspaceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListWorkspacePagesQuery, PagePagedResultDto>(
            new ListWorkspacePagesQuery(workspaceId, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/subpages")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<PageResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PageResponse>>>> GetSubPages(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await QueryAsync<ListSubPagesQuery, IReadOnlyList<PageDto>>(
            new ListSubPagesQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => (IReadOnlyList<PageResponse>)x.Select(y => y.ToResponse()).ToArray());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/pages:search")]
    [ProducesResponseType(typeof(ApiResult<PagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePagedResultResponse>>> Search(
        [FromRoute] Guid workspaceId,
        [FromQuery] string keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<SearchPagesQuery, PagePagedResultDto>(
            new SearchPagesQuery(workspaceId, keyword, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
    [HttpGet("api/v1/pages/favorites")]
    [ProducesResponseType(typeof(ApiResult<PageQuickAccessPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<PageQuickAccessPagedResultResponse>>> ListFavorites(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListFavoritePagesQuery, PageQuickAccessPagedResultDto>(
            new ListFavoritePagesQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/recent")]
    [ProducesResponseType(typeof(ApiResult<PageQuickAccessPagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    public async Task<ActionResult<ApiResult<PageQuickAccessPagedResultResponse>>> ListRecent(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListRecentPagesQuery, PageQuickAccessPagedResultDto>(
            new ListRecentPagesQuery(pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/pages/trash")]
    [ProducesResponseType(typeof(ApiResult<PagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePagedResultResponse>>> ListTrash(
        [FromRoute] Guid workspaceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync<ListArchivedPagesQuery, PagePagedResultDto>(
            new ListArchivedPagesQuery(workspaceId, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/pages/{pageId:guid}/favorite")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PageResponse>>> Favorite(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<FavoritePageCommand, PageDto>(
            new FavoritePageCommand(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/pages/{pageId:guid}/favorite")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Unfavorite(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            new UnfavoritePageCommand(pageId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("api/v1/pages/{pageId:guid}/duplicate")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> Duplicate(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<DuplicatePageCommand, PageDto>(
            new DuplicatePageCommand(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/pages/{pageId:guid}/restore")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> Restore(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync<RestorePageCommand, PageDto>(
            new RestorePageCommand(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

}
